import { createContext, FC, useContext, useState } from 'react'
import { Task } from 'models/Task'

interface IInspectionsContext {
    selectedInspectionTask: Task | undefined
    switchSelectedInspectionTask: (selectedInspectionTask: Task | undefined) => void
}

interface Props {
    children: React.ReactNode
}

const defaultInspectionsContext = {
    selectedInspectionTask: undefined,
    switchSelectedInspectionTask: () => undefined,
}

const InspectionsContext = createContext<IInspectionsContext>(defaultInspectionsContext)

export const InspectionsProvider: FC<Props> = ({ children }) => {
    const [selectedInspectionTask, setSelectedInspectionTask] = useState<Task>()

    const switchSelectedInspectionTask = (selectedTask: Task | undefined) => {
        setSelectedInspectionTask(selectedTask)
    }

    return (
        <InspectionsContext.Provider
            value={{
                selectedInspectionTask,
                switchSelectedInspectionTask,
            }}
        >
            {children}
        </InspectionsContext.Provider>
    )
}

export const useInspectionsContext = () => useContext(InspectionsContext)
