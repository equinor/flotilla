import { createContext, FC, useContext, useState } from 'react'
import { MissionStatus } from 'models/Mission'
import { InspectionType } from 'models/Inspection'

interface IMissionFilterContext {
    page: number
    switchPage: (newPage: number) => void
    filterState: {
        missionName: string | undefined
        statuses: MissionStatus[]
        robotName: string | undefined
        tagId: string | undefined
        inspectionTypes: InspectionType[]
        minStartTime: number | undefined
        maxStartTime: number | undefined
        minEndTime: number | undefined
        maxEndTime: number | undefined
    }
    filterFunctions: {
        switchMissionName: (newMissionName: string | undefined) => void
        switchStatuses: (newStatuses: MissionStatus[]) => void
        switchRobotName: (newRobotName: string | undefined) => void
        switchTagId: (newTagId: string | undefined) => void
        switchInspectionTypes: (newInspectionTypes: InspectionType[]) => void
        switchMinStartTime: (newMinStartTime: number | undefined) => void
        switchMaxStartTime: (newMaxStartTime: number | undefined) => void
        switchMinEndTime: (newMinEndTime: number | undefined) => void
        switchMaxEndTime: (newMaxEndTime: number | undefined) => void
        resetFilters: () => void
    }
}

interface Props {
    children: React.ReactNode
}

const completedStatuses = [
    MissionStatus.Aborted,
    MissionStatus.Cancelled,
    MissionStatus.Successful,
    MissionStatus.PartiallySuccessful,
    MissionStatus.Failed,
]

const defaultMissionFilterInterface = {
    page: 1,
    switchPage: (newPage: number) => {},
    filterState: {
        missionName: undefined,
        statuses: completedStatuses,
        robotName: undefined,
        tagId: undefined,
        inspectionTypes: [],
        minStartTime: undefined,
        maxStartTime: undefined,
        minEndTime: undefined,
        maxEndTime: undefined
    },
    filterFunctions: {
        switchMissionName: (newMissionName: string | undefined) => {},
        switchStatuses: (newStatuses: MissionStatus[]) => {},
        switchRobotName: (newRobotName: string | undefined) => {},
        switchTagId: (newTagId: string | undefined) => {},
        switchInspectionTypes: (newInspectionTypes: InspectionType[]) => {},
        switchMinStartTime: (newMinStartTime: number | undefined) => {},
        switchMaxStartTime: (newMaxStartTime: number | undefined) => {},
        switchMinEndTime: (newMinEndTime: number | undefined) => {},
        switchMaxEndTime: (newMaxEndTime: number | undefined) => {},
        resetFilters: () => {}
    }
}

export const MissionFilterContext = createContext<IMissionFilterContext>(defaultMissionFilterInterface)

export const MissionFilterProvider: FC<Props> = ({ children }) => {
    const [page, setPage] = useState<number>(defaultMissionFilterInterface.page)
    const [filterState, setFilterState] = useState<IMissionFilterContext["filterState"]>(defaultMissionFilterInterface.filterState)

    const switchPage = (newPage: number) => {
        setPage(newPage)
    }

    const filterFunctions = {
        switchMissionName: (newMissionName: string | undefined) => {
            setFilterState({...filterState, missionName: newMissionName})
        },
        switchStatuses: (newStatuses: MissionStatus[]) => {
            setFilterState({...filterState, statuses: newStatuses})
        },
        switchRobotName: (newRobotName: string | undefined) => {
            setFilterState({...filterState, robotName: newRobotName})
        },
        switchTagId: (newTagId: string | undefined) => {
            setFilterState({...filterState, tagId: newTagId})
        },
        switchInspectionTypes: (newInspectionTypes: InspectionType[]) => {
            setFilterState({...filterState, inspectionTypes: newInspectionTypes})
        },
        switchMinStartTime: (newMinStartTime: number | undefined) => {
            setFilterState({...filterState, minStartTime: newMinStartTime})
        },
        switchMaxStartTime: (newMaxStartTime: number | undefined) => {
            setFilterState({...filterState, maxStartTime: newMaxStartTime})
        },
        switchMinEndTime: (newMinEndTime: number | undefined) => {
            setFilterState({...filterState, minEndTime: newMinEndTime})
        },
        switchMaxEndTime: (newMaxEndTime: number | undefined) => {
            setFilterState({...filterState, maxEndTime: newMaxEndTime})
        },
        resetFilters: () => {
            setFilterState(defaultMissionFilterInterface.filterState)
        }
    }

    return (
        <MissionFilterContext.Provider
            value={{
                page,
                switchPage,
                filterState,
                filterFunctions
            }}
        >
            {children}
        </MissionFilterContext.Provider>
    )
}

export const useMissionFilterContext = () => useContext(MissionFilterContext)
