import { createContext, FC, useContext, useState } from 'react'
import { MissionStatus } from 'models/Mission'
import { InspectionType } from 'models/Inspection'

interface IMissionFilterContext {
    page: number
    switchPage: (newPage: number) => void
    missionName: string | undefined
    switchMissionName: (newMissionName: string | undefined) => void
    statuses: MissionStatus[]
    switchStatuses: (newStatuses: MissionStatus[]) => void
    robotName: string | undefined
    switchRobotName: (newRobotName: string | undefined) => void
    tagId: string | undefined
    switchTagId: (newTagId: string | undefined) => void
    inspectionTypes: InspectionType[]
    switchInspectionTypes: (newInspectionTypes: InspectionType[]) => void
    minStartTime: number | undefined
    switchMinStartTime: (newMinStartTime: number | undefined) => void
    maxStartTime: number | undefined
    switchMaxStartTime: (newMaxStartTime: number | undefined) => void
    minEndTime: number | undefined
    switchMinEndTime: (newMinEndTime: number | undefined) => void
    maxEndTime: number | undefined
    switchMaxEndTime: (newMaxEndTime: number | undefined) => void
    resetFilters: () => void
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
    missionName: undefined,
    switchMissionName: (newMissionName: string | undefined) => {},
    statuses: completedStatuses,
    switchStatuses: (newStatuses: MissionStatus[]) => {},
    robotName: undefined,
    switchRobotName: (newRobotName: string | undefined) => {},
    tagId: undefined,
    switchTagId: (newTagId: string | undefined) => {},
    inspectionTypes: [],
    switchInspectionTypes: (newInspectionTypes: InspectionType[]) => {},
    minStartTime: undefined,
    switchMinStartTime: (newMinStartTime: number | undefined) => {},
    maxStartTime: undefined,
    switchMaxStartTime: (newMaxStartTime: number | undefined) => {},
    minEndTime: undefined,
    switchMinEndTime: (newMinEndTime: number | undefined) => {},
    maxEndTime: undefined,
    switchMaxEndTime: (newMaxEndTime: number | undefined) => {},
    resetFilters: () => {},
}

export const MissionFilterContext = createContext<IMissionFilterContext>(defaultMissionFilterInterface)

export const MissionFilterProvider: FC<Props> = ({ children }) => {
    const [page, setPage] = useState<number>(1)
    const [missionName, setMissionName] = useState<string>()
    const [statuses, setStatuses] = useState<MissionStatus[]>(completedStatuses)
    const [robotName, setRobotName] = useState<string>()
    const [tagId, setTagId] = useState<string>()
    const [inspectionTypes, setInspectionTypes] = useState<InspectionType[]>([])
    const [minStartTime, setMinStartTime] = useState<number>()
    const [maxStartTime, setMaxStartTime] = useState<number>()
    const [minEndTime, setMinEndTime] = useState<number>()
    const [maxEndTime, setMaxEndTime] = useState<number>()

    const switchPage = (newPage: number) => {
        setPage(newPage)
    }

    const switchMissionName = (newMissionName: string | undefined) => {
        setMissionName(newMissionName)
    }

    const switchStatuses = (newStatuses: MissionStatus[]) => {
        setStatuses(newStatuses)
    }

    const switchRobotName = (newRobotName: string | undefined) => {
        setRobotName(newRobotName)
    }

    const switchTagId = (newTagId: string | undefined) => {
        setTagId(newTagId)
    }

    const switchInspectionTypes = (newInspectionTypes: InspectionType[]) => {
        setInspectionTypes(newInspectionTypes)
    }

    const switchMinStartTime = (newMinStartTime: number | undefined) => {
        setMinStartTime(newMinStartTime)
    }

    const switchMaxStartTime = (newMaxStartTime: number | undefined) => {
        setMaxStartTime(newMaxStartTime)
    }

    const switchMinEndTime = (newMinEndTime: number | undefined) => {
        setMinEndTime(newMinEndTime)
    }

    const switchMaxEndTime = (newMaxEndTime: number | undefined) => {
        setMaxEndTime(newMaxEndTime)
    }

    const resetFilters = () => {
        switchMissionName(undefined)
        switchStatuses(completedStatuses)
        switchRobotName(undefined)
        switchTagId(undefined)
        switchInspectionTypes([])
        switchMinStartTime(undefined)
        switchMaxStartTime(undefined)
        switchMinEndTime(undefined)
        switchMaxEndTime(undefined)
    }

    return (
        <MissionFilterContext.Provider
            value={{
                page,
                switchPage,
                missionName,
                switchMissionName,
                statuses,
                switchStatuses,
                robotName,
                switchRobotName,
                tagId,
                switchTagId,
                inspectionTypes,
                switchInspectionTypes,
                minStartTime,
                switchMinStartTime,
                maxStartTime,
                switchMaxStartTime,
                minEndTime,
                switchMinEndTime,
                maxEndTime,
                switchMaxEndTime,
                resetFilters,
            }}
        >
            {children}
        </MissionFilterContext.Provider>
    )
}

export const useMissionFilterContext = () => useContext(MissionFilterContext)
