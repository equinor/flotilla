import { createContext, FC, useContext, useEffect, useState } from 'react'
import { MissionStatus } from 'models/Mission'
import { InspectionType } from 'models/Inspection'
import { useLanguageContext } from './LanguageContext'

interface IMissionFilterContext {
    page: number
    switchPage: (newPage: number) => void
    filterIsSet: boolean
    filterError: string
    clearFilterError: () => void
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
        resetFilter: (s: string) => void
        removeFilter: (s: string) => void
        isDefault: (filterName: string, value: any) => boolean
        removeFilterElement: (filterName: string, value: any) => void
        dateTimeStringToInt: (dateTimeString: string | undefined) => number | undefined
        dateTimeIntToString: (dateTimeNumber: number | undefined) => string | undefined
        dateTimeIntToPrettyString: (dateTimeNumber: number | undefined) => string | undefined
    }
}

export type IFilterState = IMissionFilterContext['filterState']

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
    filterIsSet: false,
    filterError: '',
    clearFilterError: () => {},
    filterState: {
        missionName: undefined,
        statuses: completedStatuses,
        robotName: undefined,
        tagId: undefined,
        inspectionTypes: [],
        minStartTime: undefined,
        maxStartTime: undefined,
        minEndTime: undefined,
        maxEndTime: undefined,
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
        resetFilters: () => {},
        resetFilter: (s: string) => {},
        removeFilter: (s: string) => {},
        isDefault: (filterName: string, value: any) => true,
        removeFilterElement: (filterName: string, value: any) => {},
        dateTimeStringToInt: (dateTimeString: string | undefined) => 0,
        dateTimeIntToString: (dateTimeNumber: number | undefined) => '',
        dateTimeIntToPrettyString: (dateTimeNumber: number | undefined) => '',
    },
}

export const MissionFilterContext = createContext<IMissionFilterContext>(defaultMissionFilterInterface)

export const MissionFilterProvider: FC<Props> = ({ children }) => {
    const { TranslateText } = useLanguageContext()
    const [page, setPage] = useState<number>(defaultMissionFilterInterface.page)
    const [filterError, setFilterError] = useState<string>(defaultMissionFilterInterface.filterError)
    const [filterIsSet, setFilterIsSet] = useState<boolean>(defaultMissionFilterInterface.filterIsSet)
    const [filterState, setFilterState] = useState<IMissionFilterContext['filterState']>(
        defaultMissionFilterInterface.filterState
    )

    const switchPage = (newPage: number) => {
        setPage(newPage)
    }

    const clearFilterError = () => {
        setFilterError('')
    }

    const filterFunctions = {
        switchMissionName: (newMissionName: string | undefined) => {
            setFilterIsSet(true)
            setFilterState({ ...filterState, missionName: newMissionName })
        },
        switchStatuses: (newStatuses: MissionStatus[]) => {
            setFilterIsSet(true)
            setFilterState({ ...filterState, statuses: newStatuses })
        },
        switchRobotName: (newRobotName: string | undefined) => {
            setFilterIsSet(true)
            setFilterState({ ...filterState, robotName: newRobotName })
        },
        switchTagId: (newTagId: string | undefined) => {
            setFilterIsSet(true)
            setFilterState({ ...filterState, tagId: newTagId })
        },
        switchInspectionTypes: (newInspectionTypes: InspectionType[]) => {
            setFilterIsSet(true)
            setFilterState({ ...filterState, inspectionTypes: newInspectionTypes })
        },
        switchMinStartTime: (newMinStartTime: number | undefined) => {
            if (
                filterState.minEndTime &&
                newMinStartTime &&
                filterState.maxStartTime &&
                (newMinStartTime > filterState.minEndTime || newMinStartTime > filterState.maxStartTime)
            )
                setFilterError(
                    TranslateText('minStartTime') +
                        ' ' +
                        TranslateText('cannot be greater than') +
                        ' ' +
                        TranslateText('minEndTime') +
                        ' ' +
                        TranslateText('or') +
                        TranslateText('maxStartTime')
                )
            else {
                setFilterIsSet(true)
                setFilterState({ ...filterState, minStartTime: newMinStartTime })
            }
        },
        switchMaxStartTime: (newMaxStartTime: number | undefined) => {
            if (filterState.maxEndTime && newMaxStartTime && newMaxStartTime > filterState.maxEndTime)
                setFilterError(
                    TranslateText('maxStartTime') +
                        ' ' +
                        TranslateText('cannot be greater than') +
                        ' ' +
                        TranslateText('maxEndTime')
                )
            if (filterState.minEndTime && newMaxStartTime && newMaxStartTime < filterState.minEndTime)
                setFilterError(
                    TranslateText('maxStartTime') +
                        ' ' +
                        TranslateText('cannot be less than') +
                        ' ' +
                        TranslateText('minStartTime')
                )
            else {
                setFilterIsSet(true)
                setFilterState({ ...filterState, maxStartTime: newMaxStartTime })
            }
        },
        switchMinEndTime: (newMinEndTime: number | undefined) => {
            if (filterState.maxEndTime && newMinEndTime && newMinEndTime > filterState.maxEndTime)
                setFilterError(
                    TranslateText('minEndTime') +
                        ' ' +
                        TranslateText('cannot be greater than') +
                        ' ' +
                        TranslateText('maxEndTime')
                )
            if (filterState.minStartTime && newMinEndTime && newMinEndTime < filterState.minStartTime)
                setFilterError(
                    TranslateText('minEndTime') +
                        ' ' +
                        TranslateText('cannot be less than') +
                        ' ' +
                        TranslateText('minStartTime')
                )
            else {
                setFilterIsSet(true)
                setFilterState({ ...filterState, minEndTime: newMinEndTime })
            }
        },
        switchMaxEndTime: (newMaxEndTime: number | undefined) => {
            if (
                filterState.maxStartTime &&
                newMaxEndTime &&
                filterState.minEndTime &&
                (newMaxEndTime < filterState.maxStartTime || newMaxEndTime < filterState.minEndTime)
            )
                setFilterError(
                    TranslateText('maxEndTime') +
                        ' ' +
                        TranslateText('cannot be less than') +
                        ' ' +
                        TranslateText('minEndTime') +
                        ' ' +
                        TranslateText('or') +
                        TranslateText('minEndTime')
                )
            else {
                setFilterIsSet(true)
                setFilterState({ ...filterState, maxEndTime: newMaxEndTime })
            }
        },
        resetFilters: () => {
            setFilterIsSet(false)
            setFilterState(defaultMissionFilterInterface.filterState)
        },
        resetFilter(filterName: string) {
            filterName = filterName.trim()
            const defaultState = defaultMissionFilterInterface.filterState
            const defaultValue = defaultState[filterName as keyof typeof defaultState]
            setFilterState({ ...filterState, [filterName]: defaultValue })
        },
        removeFilter(filterName: string) {
            filterName = filterName.trim()
            const defaultState = defaultMissionFilterInterface.filterState
            const defaultValue = defaultState[filterName as keyof typeof defaultState]
            const newValue = Array.isArray(defaultValue) ? [] : undefined
            setFilterState({ ...filterState, [filterName]: newValue })
        },
        removeFilterElement(filterName: string, value: any) {
            filterName = filterName.trim()
            if (!Object.keys(filterState).includes(filterName)) return
            const currentArray = filterState[filterName as keyof typeof filterState] as any[]
            if (!Array.isArray(currentArray)) return
            let newArray = currentArray.filter((val) => val !== value)
            setFilterState({ ...filterState, [filterName]: newArray })
        },
        isDefault(filterName: string, value: any) {
            filterName = filterName.trim()
            if (!Object.keys(filterState).includes(filterName)) return false
            const defaultState = defaultMissionFilterInterface.filterState
            const defaultValue = defaultState[filterName as keyof typeof defaultState]
            if (Array.isArray(defaultValue)) {
                return defaultValue.length === value.length && [...defaultValue].every((x) => value.includes(x))
            } else {
                return defaultValue === value
            }
        },
        dateTimeStringToInt: (dateTimeString: string | undefined) => {
            if (dateTimeString === '' || dateTimeString === undefined) return undefined
            return new Date(dateTimeString).getTime() / 1000
        },
        dateTimeIntToString: (dateTimeNumber: number | undefined) => {
            if (dateTimeNumber === 0 || dateTimeNumber === undefined) return undefined
            const t = new Date(dateTimeNumber * 1000)
            const z = new Date(t.getTimezoneOffset() * 60 * 1000)
            const tLocal = new Date(t.getTime() - z.getTime())
            const tLocalDate = new Date(tLocal)
            let iso = tLocalDate.toISOString()
            iso = iso.split('.')[0]
            return iso.slice(0, -3) // Removes :00 at the end
        },
        dateTimeIntToPrettyString: (dateTimeNumber: number | undefined) => {
            if (dateTimeNumber === 0 || dateTimeNumber === undefined) return undefined
            const t = new Date(dateTimeNumber * 1000)
            const z = new Date(t.getTimezoneOffset() * 60 * 1000)
            const tLocal = new Date(t.getTime() - z.getTime())
            const tLocalDate = new Date(tLocal)
            let iso = tLocalDate.toISOString()
            iso = iso.split('.')[0]
            iso = iso.replace('T', ' ')
            return iso.slice(0, -3) // Removes :00 at the end
        },
    }

    const isAllDefault = () => {
        if (Object.keys(filterState).length !== Object.keys(defaultMissionFilterInterface.filterState).length)
            return false
        return Object.entries(filterState).every((entry) => filterFunctions.isDefault(entry[0], entry[1]))
    }

    useEffect(() => {
        if (isAllDefault()) setFilterIsSet(false)
    }, [filterState])

    return (
        <MissionFilterContext.Provider
            value={{
                page,
                switchPage,
                filterState,
                filterFunctions,
                filterIsSet,
                filterError,
                clearFilterError,
            }}
        >
            {children}
        </MissionFilterContext.Provider>
    )
}

export const useMissionFilterContext = () => useContext(MissionFilterContext)
