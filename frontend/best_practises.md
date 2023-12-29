# Flotilla frontend development best practises

- [Flotilla frontend development best practises](#flotilla-frontend-development-best-practises)
  - [Setup](#setup)
    - [Prettier](#prettier)
    - [ESLint](#eslint)
  - [Folder structure](#folder-structure)
  - [Components](#react-components)
    - [React arguments](#react-arguments)
    - [React functions](#react-state)
    - [React state](#react-state)
    - [Nesting](#nesting)
  - [Contexts](#contexts)
  - [SignalR](#signalr)
  - [Functional programming](#functional-programming)
  - [Declarative programming](#declarative-programming)
    - [Input](#input)

## Setup

See the [README](./README.md) for more information.

### Prettier

We abide by the formatting provided by Prettier. To run it, type 
    npx prettier --write [path to source]

### ESLint

We also avoid any warnings or errors from ESLint before we merge in any code. These warnings appear
when compiling the code using
    npm start
but can also be run with
    npx eslint [path to src]

## Folder structure

The frontend src folder is organised into 6 main folders.
- Alerts contains code which displays alerts on the top of the page
- Contexts contain react contexts (see [the context section for more information](#contexts))
- Displays contain visual react component which are used on more than one page
- Header contains code related to the page header
- Pages contains the bulk of the code, as all the code related to the website pages are kept here, if there are no other relevant folders
- Language contains translations between the supported languages for text on the Flotilla web page
- MediaAssets contains the static image files displayed on the page
- Models contains the data models
- Utils contain utility functions which are relevant in several parts of the code

## Function syntax

In this code we generally avoid using the "function" for the sake of consistency. This means that if you wish to define a function, you should define a variable which is set to the value of a lambda function. Therefore

    function foo(x: number, y: number) { return x + y }

becomes

    const foo = (x:number, y: number) => { return x + y }

It is also preferred to simplify the syntax when possible to make it more readable. When the code in a codeblock only requires one line for instance, the {} brackets may be ommited, along with the return statement. So the function above becomes

    const foo = (x:number, y: number) => x + y

## React components

The flotilla frontend is programmed in Typescript using the React framework. In this framework all HTML components are defined using React components, which are pieces of code which define what the HTML should look like depending on the state of the system. We only utilise React Functions, which means that components are described in the form of functions which accept the component attributes are function arguments, and return the HTML at the end of the function. React classes should be avoided.

### React arguments

React arguments should be descriptively named in relation to the components name, so that someone would not need to investigate the parent component to learn what the data represents. If only parts of the provided data is utilised, then it is often better to only send the relevant data (eg only send mission name instead of the whole mission if only the name is used). Additionally, an interface should be utilised when there are many components, so as to make it easier to read. This interface should be placed just above, or at least nearby, the function itself.

Good (although the interface is not strictly neccessary here):
```
interface MissionTitleComponentProps {
    missionName: string
}

export const MissionTitleComponent = ({ missionName } : MissionTitleComponentProps) => {
    ...
}
```

Bad:
```
export const RobotComponent = ({ mission, setColor } : {mission: CondensedMissionDefinition, setColor: (c: string) => void}) => {
    ...
}
```

### React state

React provides the useState hook which allows the user to define variables which persist between renders. Although it is tempting to use these for all variables, it is worth noting that unless the variable has some information which needs to be kept for another render in order to update another variable, it can be kept as a normal Typescript variable.

So if you have a simple component as such:

```
const ExampleComponent = ({ x: number } : { x: number }) => {
    const [numberToDisplay, setNumberToDisplay] = useState<number>(0)

    useEffect(() => {
        setNumberToDisplay(x + 1)
    }, [x])
    return (
        <div>{numberToDisplay}</div>
    )
}
```
it can be simplified to 
```
const ExampleComponent = ({ x: number } : { x: number }) => {
    setNumberToDisplay = x + 1
    return (
        <div>{numberToDisplay}</div>
    )
}
```
although in this contrived example it would be easier to give a more meaningful name to 'x' and then write
```
const ExampleComponent = ({ x: number } : { x: number }) => <div>{x + 1}</div>
```

It is also worth noting that calls to the update functions of react state are grouped together at the end of each render. So in the following code:
```
const [x, setX] = useState<number>(0)

useEffect(() => {
    console.log(x) // 0
    setX(x + 1)
    console.log(x) // still 0
    set(x + 1)
}, [x])
```
we do not yet see the updated value of x in the same render we updated it. Additionally, if we call setX several times in one render (ie. inside the same useEffect), they will overwrite each other. 'x' will be 1 at the end of this render, not 2. In order to prevent this overwriting we can instead pass a function to the set function which describes how to update the variable using its current value. This will be done in turn for each call to setX in this case, prevent updates from being overwritten.
```
const [x, setX] = useState<number>(0)

useEffect(() => {
    setX((oldX) => oldX + 1)
    setX((oldX) => oldX + 1)
}, [x])
```
At the end of the above code the value of 'x' will be 2, as the second update call will use the output from the first update call as the input to its function. This is important to keep in mind in event handlers, as the state inside for instance signalR event handlers is frozen when they are first registered.

### Nesting

React components naturally include other react components within their HTML return statement. If these nested components are rather large, or if they are not strictly related to the name of the file you are currently in, it is better to include these components in their own file. If there are several nested components that are needed for a certain section, or if the nested component has some small related files, then it can be good to place these component files in a nested folder.

If a component is placed away from its parent it is important to try to remain a loose coupling between these two. This means that there should not be a lot of arguments provided to the child component, and the arguments that are provided should make sense in the context of the child component without needing to read through the code of the parent component. In general, each component should be treated as independent, not only because we may want to reuse the component, but also because this makes the component easier to interpret and update for other developers. If the large number of arguments are not avoidable, it might be best to either not separate the component away from the parent, or some refactoring of the parent may instead be needed.

## Contexts

React contexts are an important tool to maintain state across components. They work as react components which do not display any HTML but instead just pass on the HTML of their parent. This component (called 'provider') is then placed high up in the program structure, such as in App.ts. The react context which the provider exports can then be referred to by any components nested below it. This is done using the useContext react hook, which returns the context of any parent of the current component. It is generally best to abstract over this function call in the provider in order to give it a better name, eg useRobotContext, as opposed to having to find the right context when calling useContext.

It is important to remember to include the provider in the top level of the program, and to remember that contexts cannot see other contexts whose providers are lower than them in the hierarchy.

The main use of contexts is to store state which is used in more than one component. Any react state defined in the context can be imported in other components, and this state will be identical for each of them. This is vital to allow the state to remain the same whilst moving between pages, as the data in the react components would otherwise be reset each time they stop being rendered. 

Treating the contexts as data aggregates also simplifies each component as all the code related to fetching and formatting data from the backend can be moved to contexts. In particular it is ideal to move signalR event handlers to contexts as these can make components difficult to read otherwise. In effect we treat the contexts as light versions of redux stores, where we fetch the data from the context, and then the context can also expose functions which allow us to update the state in the context. This allows us to better control what data is visible and how it is possible to update it. A great example of this can be seen in [the mission filter context](./src/components/Contexts/MissionFilterContext.tsx) and in [the filter component where it's used](./src/components/Pages/MissionHistoryPage/FilterSection.tsx).


## SignalR

Information on the best practises related to SignalR can be found in [the signalR context](./src/components/Contexts/SignalRContext.tsx).

## Functional programming

Functional programming is a large field, but for the sake of this document we are interested in containing side effects within functions. This means that we do not want functions to change any state other that the arguments provided. The actual objects sent as arguments should also not be changed themselves, instead the result of performing manipulations on them should be returned as a new state at the end of the function. This form of self contained function is called a pure function.

In react there are two main side-effects, updating react state using set functions, and sending/receiving data to/from the backend. This is not avoidable, but we can contain them to be done inside useEffects. In these useEffects the side-effects should be performed at the top level instead of inside nested calls. Functions calls can be made inside the useEffects for the sake of manipulation of the data and formatting, but the end result should be returned to the useEffect before a set call is made. In effect, no functions should have a 'void' return type when possible. Setting state inside event handlers is another avoidable situation, besides useEffects. 

Making the code as functional as possible does not only make it more readable, it also reduces the chances of errors being introduced as it forces us to make many simple functions which do isolated operations. Additionally it moves the state operations to one place, making any previous mistakes more obvious.

Using coding styles common in functional languages are also encouraged in Typescript. This involves using data pipelines, such as map/filter/reduce/etc. If formatted on several lines it can be just as readable as using temporary variables, but this should be done within reason. Temporary variables are not neccessarily worse than doing the above approach, as long the as the temporary variables are kept within the scope of a pure function.

In the following examples the functions are kept simple for the sake of demonstration.

Bad (all updates are kept obfuscated inside the updateLists function):
```
const [x, setX] = useState<number>(0)
const [numberList, setNumberList] = useState<number[]>(0)
const [otherNumberList, setOtherNumberList] = useState<number[]>(0)

const updateLists = () => {
    let currentX = x
    let numberListCopy = [...numberList]
    let xIndex = numberList.findIndex(currentX)
    numberListCopy.splice(xIndex, 1)
    setNumberList(numberListCopy)

    let otherNumberListCopy = [...otherNumberList]
    otherNumberListCopy.push(currentX)
    setOtherNumberList(otherNumberListCopy)
}

useEffect(() => {
    updateLists()
}, [x, numberList, otherNumberList])
```

Better (the updates of each list is separated, and the number is passed as an argument):
```
const [x, setX] = useState<number>(0)
const [numberList, setNumberList] = useState<number[]>(0)
const [otherNumberList, setOtherNumberList] = useState<number[]>(0)

const removeNumberFromNumberList = (y: number) => {
    let numberListCopy = [...numberList]
    let yIndex = numberList.findIndex(y)
    numberListCopy.splice(yIndex, 1)
    setNumberList(numberListCopy)
}

const insertNumberInOtherNumberList = (y: number) => {
    let otherNumberListCopy = [...otherNumberList]
    otherNumberListCopy.push(y)
    setOtherNumberList(otherNumberListCopy)
}

useEffect(() => {
    removeNumberFromNumberList(x)
    insertNumberInOtherNumberList(x)
}, [x, numberList, otherNumberList])
```

Best (The functions only manipulate the given arguments and does not access any react state directly, whilst all the state updates are being done in the useEffect on the top level):
```
const [x, setX] = useState<number>(0)
const [numberList, setNumberList] = useState<number[]>(0)
const [otherNumberList, setOtherNumberList] = useState<number[]>(0)

const removeNumberFromNumberList = (y: number, list: number[]) => {
    let listCopy = [...list]
    let yIndex = listCopy.findIndex(y)
    listCopy.splice(yIndex, 1)
    return listCopy
}

const insertNumberInOtherNumberList = (y: number, list: number[]) => {
    let listcopy = [...list]
    listcopy.push(y)
    return listCopy
}

useEffect(() => {
    setNumberList(removeNumberFromList(x, numberList))
    setOtherNumberList(insertNumberIntoList(x, otherNumberList))
}, [x, numberList, otherNumberList])
```

If we do the following then we also prevent any conflicts that would arise from having multiple updates to numberList and otherNumberList in the same render, but that is not the case in this simple example.
```
useEffect(() => {
    setNumberList((oldList) => removeNumberFromList(x, oldList))
    setOtherNumberList((oldList) => insertNumberIntoList(x, oldList))
}, [x, numberList, otherNumberList])
```

## Declarative programming

React is a naturally declarative framwork. Declarative programming is a programming paradigm where we define what the result of the code running should be, instead of explicitly explaining how this should be achieved step-by-step. When making a react component we for instance do not state the order in which things should be rendered, instead we provide the HTML we wish to render, and refer to other components which hide further complexity behind them.

In general it is good to minimise the number of function calls that need to be made inside the HTML content. Instead of calling a function which returns some HTMl, it is better to make the function into a react component and then simply place it in the HTML as a HTML element. For even handlers we can also pass descriptively named event handlers as objects, instead of defining there the code which should be run.

It is also good to not be afraid to define local small react components inside react component functions. Additionally, instead of writing code inside useEffects which define how the data of one useState variable can be converted into another useState variable, it is best to limit the number of react state variables and instead define subsequent variables in terms of the first variable. This can be done in conjunction with functional programming techniques, by for example describing how one variable maps to another using map/filter/reduce functions.

Here are some examples for good and bad practises. 'getIndexDisplay' is small enough that it would not need to be separated from the react function return statement, but here it is just used as an example.

Bad (A multiline function being called inside the HTML object):
```
const FooComponent = ({ index }: { index: number }) => {
    const getIndexDisplay(x: number) => {
        return <p>{x + 1}</p>
    }

    return (
        <div>{getIndexDisplay(index)}</div>
    )
}
```

Better (The function has been inlined so that it becomes a simple mapping from input to output):
```
const FooComponent = ({ index }: { index: number }) => {
    const getIndexDisplay(x: number) => <p>{x + 1}</p>

    return (
        <div>{getIndexDisplay(index)}</div>
    )
}
```

Good (The function has become a react component, so that it does not need to be excplicitly called):
```
const FooComponent = ({ index }: { index: number }) => {
    const IndexDisplay({ x: number }: { x: number }) => <p>{x + 1}</p>

    return (
        <div><IndexDisplay x={index} /></div>
    )
}
```

### Input

It can be tempting to use temporary variables which are updated whenever a change to an input component is detected, and to keep this separate from the input component itself. However, react supports so-called "controlled components", which are components where we set the value of the input component to be the same as the variable that is updated when a change is detected. React automatically deals with this circular definition, allowing us to have a variable which both tells us when the input changes, whilst also telling us what the input box contains at any given time.

Here is an example of a controlled input component:

```
<Search
    value={filterState.tagId ?? ''}
    placeholder={TranslateText('Search for a tag')}
    onChange={(changes: ChangeEvent<HTMLInputElement>) => {
        filterFunctions.switchTagId(changes.target.value)
    }}
/>
```
The filterFunctions.switchTagId function sets the state of filterState.tagId
