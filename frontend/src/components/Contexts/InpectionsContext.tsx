import { createContext, FC, useContext, useState, useEffect, useRef } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Task } from 'models/Task'
import { useInstallationContext } from './InstallationContext'

interface IInspectionsContext {
    selectedInspectionTask: Task | undefined
    selectedInspectionTasks: Task[]
    switchSelectedInspectionTask: (selectedInspectionTask: Task | undefined) => void
    switchSelectedInspectionTasks: (selectedInspectionTask: Task[]) => void
    mappingInspectionTasksObjectURL: { [taskIsarId: string]: string }
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    selectedInspectionTask: undefined,
    selectedInspectionTasks: [],
    switchSelectedInspectionTask: (selectedInspectionTask: Task | undefined) => undefined,
    switchSelectedInspectionTasks: (selectedInspectionTasks: Task[]) => [],
    mappingInspectionTasksObjectURL: {},
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const { installationCode } = useInstallationContext()

    const [selectedInspectionTask, setSelectedInspectionTask] = useState<Task>()
    const [selectedInspectionTasks, setSelectedInspectionTasks] = useState<Task[]>([])
    const [selectedInspectionTasksToFetch, setSelectedInspectionTasksToFetch] = useState<Task[]>([])

    const [mappingInspectionTasksObjectURL, setMappingInspectionTasksObjectURL] = useState<{
        [taskId: string]: string
    }>({})

    const [triggerFetch, setTriggerFetch] = useState<boolean>(false)
    const [startTimer, setStartTimer] = useState<boolean>(false)
    const imageObjectURL = useRef<string>('')

    useEffect(() => {
        const timeoutId = setTimeout(() => {
            if (selectedInspectionTasksToFetch.length > 0)
                setTriggerFetch((oldSetTriggerToFetch) => !oldSetTriggerToFetch)
        }, 10000)
        return () => clearTimeout(timeoutId)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [startTimer])

    useEffect(() => {
        Object.values(selectedInspectionTasksToFetch).forEach((task, index) => {
            if (task.isarTaskId) {
                BackendAPICaller.getInspection(installationCode, task.isarTaskId!)
                    .then((imageBlob) => {
                        imageObjectURL.current = URL.createObjectURL(imageBlob)
                    })
                    .then(() => {
                        setMappingInspectionTasksObjectURL((oldMappingInspectionTasksObjectURL) => {
                            return { ...oldMappingInspectionTasksObjectURL, [task.isarTaskId!]: imageObjectURL.current }
                        })
                        setSelectedInspectionTasksToFetch((oldSelectedInspectionTasksToFetch) => {
                            let newInspectionTaksToFetch = { ...oldSelectedInspectionTasksToFetch }
                            delete newInspectionTaksToFetch[index]
                            return newInspectionTaksToFetch
                        })
                    })
                    .catch(() => {
                        setStartTimer((oldValue) => !oldValue)
                    })
            }
        })
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [installationCode, selectedInspectionTasksToFetch, triggerFetch])

    const switchSelectedInspectionTask = (selectedName: Task | undefined) => {
        setSelectedInspectionTask(selectedName)
    }

    const switchSelectedInspectionTasks = (selectedName: Task[]) => {
        setMappingInspectionTasksObjectURL({})
        setSelectedInspectionTasks(selectedName)
        setSelectedInspectionTasksToFetch(selectedName)
    }

    return (
        <InspectionsContext.Provider
            value={{
                selectedInspectionTask,
                selectedInspectionTasks,
                switchSelectedInspectionTask,
                switchSelectedInspectionTasks,
                mappingInspectionTasksObjectURL,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
