import { createContext, FC, useContext, useEffect, useState, useMemo } from 'react'
import { MissionStatusFilterOptions, missionStatusFilterOptionsIterable } from 'models/Mission'
import { InspectionType } from 'models/Inspection'
import { useLanguageContext } from './LanguageContext'
import { MissionRunQueryParameters } from 'models/MissionRunQueryParameters'
import { useSearchParams } from 'react-router-dom'

interface IMissionFilterContext {
    page: number
    switchPage: (newPage: number) => void
    filterIsSet: boolean
    filterError: string
    clearFilterError: () => void
    filterState: {
        missionName: string | undefined
        statuses: MissionStatusFilterOptions[] | undefined
        robotName: string | undefined
        tagId: string | undefined
        inspectionTypes: InspectionType[] | undefined
        minStartTime: number | undefined
        maxStartTime: number | undefined
        minEndTime: number | undefined
        maxEndTime: number | undefined
    }
    filterFunctions: {
        switchMissionName: (newMissionName: string | undefined) => void
        switchStatuses: (newStatuses: MissionStatusFilterOptions[]) => void
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
        removeFilters: () => void
        isDefault: (filterName: string, value: any) => boolean
        isSet: (filterName: string, value: any) => boolean
        removeFilterElement: (filterName: string, value: any) => void
        dateTimeStringToInt: (dateTimeString: string | undefined) => number | undefined
        dateTimeIntToString: (dateTimeNumber: number | undefined) => string | undefined
        dateTimeIntToPrettyString: (dateTimeNumber: number | undefined) => string | undefined
        getFormattedFilter: () => MissionRunQueryParameters | undefined
    }
}

export type IFilterState = IMissionFilterContext['filterState']

interface Props {
    children: React.ReactNode
}

const defaultMissionFilterInterface: IMissionFilterContext = {
    page: 1,
    switchPage: () => {},
    filterIsSet: true,
    filterError: '',
    clearFilterError: () => {},
    filterState: {
        missionName: undefined,
        statuses: undefined,
        robotName: undefined,
        tagId: undefined,
        inspectionTypes: undefined,
        minStartTime: undefined,
        maxStartTime: undefined,
        minEndTime: undefined,
        maxEndTime: undefined,
    },
    filterFunctions: {
        switchMissionName: () => {},
        switchStatuses: () => {},
        switchRobotName: () => {},
        switchTagId: () => {},
        switchInspectionTypes: () => {},
        switchMinStartTime: () => {},
        switchMaxStartTime: () => {},
        switchMinEndTime: () => {},
        switchMaxEndTime: () => {},
        resetFilters: () => {},
        resetFilter: () => {},
        removeFilter: () => {},
        removeFilters: () => {},
        isDefault: () => true,
        isSet: () => true,
        removeFilterElement: () => {},
        dateTimeStringToInt: () => 0,
        dateTimeIntToString: () => '',
        dateTimeIntToPrettyString: () => '',
        getFormattedFilter: () => undefined,
    },
}

const mapURLtoFilter = (searchParams: URLSearchParams): IMissionFilterContext['filterState'] => {
    const newFilter: IMissionFilterContext['filterState'] = defaultMissionFilterInterface.filterState
    searchParams.entries().forEach((searchParam: [string, string]) => {
        if (!searchParam[1] || searchParam[1] === '[]') return
        try {
            const parsedParams = JSON.parse(searchParam[1])
            newFilter[searchParam[0] as keyof typeof newFilter] = parsedParams as any
        } catch {
            return
        }
    })
    return newFilter
}

const MissionFilterContext = createContext<IMissionFilterContext>(defaultMissionFilterInterface)

export const MissionFilterProvider: FC<Props> = ({ children }) => {
    const { TranslateText } = useLanguageContext()
    const [searchParams, setSearchParams] = useSearchParams()
    const [page, setPage] = useState<number>(defaultMissionFilterInterface.page)
    const [filterError, setFilterError] = useState<string>(defaultMissionFilterInterface.filterError)
    const [filterIsSet, setFilterIsSet] = useState<boolean>(defaultMissionFilterInterface.filterIsSet)
    const [filterState, setFilterState] = useState<IMissionFilterContext['filterState']>(mapURLtoFilter(searchParams))

    useEffect(() => {
        const newParams = new URLSearchParams()
        Object.entries(filterState).forEach((entry) => {
            if (entry[1]) newParams.set(entry[0], JSON.stringify(entry[1]))
        })
        setSearchParams(newParams, { replace: true })
    }, [filterState])

    const switchPage = (newPage: number) => {
        setPage(newPage)
    }

    const clearFilterError = () => {
        setFilterError('')
    }

    const filterFunctions = useMemo(
        () => ({
            switchMissionName: (newMissionName: string | undefined) => {
                setFilterIsSet(true)
                setFilterState({ ...filterState, missionName: newMissionName })
            },
            switchStatuses: (newStatuses: MissionStatusFilterOptions[]) => {
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
                    (newMinStartTime && filterState.minEndTime && newMinStartTime > filterState.minEndTime) ||
                    (filterState.maxStartTime && newMinStartTime! > filterState.maxStartTime)
                )
                    setFilterError(
                        TranslateText('{0} cannot be greater than {1} or {2}', [
                            TranslateText('minStartTime'),
                            TranslateText('minEndTime'),
                            TranslateText('maxStartTime').toLowerCase(),
                        ])
                    )
                else {
                    setFilterIsSet(true)
                    setFilterState({ ...filterState, minStartTime: newMinStartTime })
                }
            },
            switchMaxStartTime: (newMaxStartTime: number | undefined) => {
                if (filterState.maxEndTime && newMaxStartTime && newMaxStartTime > filterState.maxEndTime)
                    setFilterError(
                        TranslateText('{0} cannot be greater than {1}', [
                            TranslateText('maxStartTime'),
                            TranslateText('maxEndTime').toLowerCase(),
                        ])
                    )
                else if (filterState.minStartTime && newMaxStartTime && newMaxStartTime < filterState.minStartTime)
                    setFilterError(
                        TranslateText('{0} cannot be less than {1}', [
                            TranslateText('maxStartTime'),
                            TranslateText('minStartTime').toLowerCase(),
                        ])
                    )
                else {
                    setFilterIsSet(true)
                    setFilterState({ ...filterState, maxStartTime: newMaxStartTime })
                }
            },
            switchMinEndTime: (newMinEndTime: number | undefined) => {
                if (filterState.maxEndTime && newMinEndTime && newMinEndTime > filterState.maxEndTime)
                    setFilterError(
                        TranslateText('{0} cannot be greater than {1}', [
                            TranslateText('minEndTime'),
                            TranslateText('maxEndTime').toLowerCase(),
                        ])
                    )
                else if (filterState.minStartTime && newMinEndTime && newMinEndTime < filterState.minStartTime)
                    setFilterError(
                        TranslateText('{0} cannot be less than {1}', [
                            TranslateText('minEndTime'),
                            TranslateText('minStartTime').toLowerCase(),
                        ])
                    )
                else {
                    setFilterIsSet(true)
                    setFilterState({ ...filterState, minEndTime: newMinEndTime })
                }
            },
            switchMaxEndTime: (newMaxEndTime: number | undefined) => {
                if (
                    (newMaxEndTime && filterState.maxStartTime && newMaxEndTime < filterState.maxStartTime) ||
                    (filterState.minEndTime && newMaxEndTime! < filterState.minEndTime)
                )
                    setFilterError(
                        TranslateText('{0} cannot be less than {1} or {2}', [
                            TranslateText('maxEndTime'),
                            TranslateText('minEndTime'),
                            TranslateText('maxStartTime').toLowerCase(),
                        ])
                    )
                else {
                    setFilterIsSet(true)
                    setFilterState({ ...filterState, maxEndTime: newMaxEndTime })
                }
            },
            resetFilters: () => {
                setFilterState(defaultMissionFilterInterface.filterState)
            },
            resetFilter: (filterName: string) => {
                filterName = filterName.trim()
                const defaultState = defaultMissionFilterInterface.filterState
                const defaultValue = defaultState[filterName as keyof typeof defaultState]
                setFilterState({ ...filterState, [filterName]: defaultValue })
            },
            removeFilters: () => {
                const localFilter: IMissionFilterContext['filterState'] = { ...filterState }
                for (const key of Object.keys(localFilter)) localFilter[key as keyof typeof localFilter] = undefined

                setFilterState(localFilter)
            },
            removeFilter: (filterName: string) => {
                filterName = filterName.trim()
                setFilterState({ ...filterState, [filterName]: undefined })
            },
            removeFilterElement: (filterName: string, value: any) => {
                filterName = filterName.trim()
                if (!Object.keys(filterState).includes(filterName)) return
                const currentArray = filterState[filterName as keyof typeof filterState] as any[]
                if (!Array.isArray(currentArray)) return
                const newArray = currentArray.filter((val) => val !== value)
                setFilterState({ ...filterState, [filterName]: newArray })
            },
            isDefault: (filterName: string, value: any) => {
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
            isSet: (filterName: string, value: any) => {
                filterName = filterName.trim()
                if (!Object.keys(filterState).includes(filterName)) return false
                return !value || value.length === 0
            },
            dateTimeStringToInt: (dateTimeString: string | undefined) => {
                if (dateTimeString === '' || !dateTimeString) return undefined
                return new Date(dateTimeString).getTime() / 1000
            },
            dateTimeIntToString: (dateTimeNumber: number | undefined) => {
                if (dateTimeNumber === 0 || !dateTimeNumber) return undefined
                const t = new Date(dateTimeNumber * 1000)
                const z = new Date(t.getTimezoneOffset() * 60 * 1000)
                const tLocal = new Date(t.getTime() - z.getTime())
                const tLocalDate = new Date(tLocal)
                let iso = tLocalDate.toISOString()
                iso = iso.split('.')[0]
                return iso.slice(0, -3) // Removes :00 at the end
            },
            dateTimeIntToPrettyString: (dateTimeNumber: number | undefined) => {
                if (dateTimeNumber === 0 || !dateTimeNumber) return undefined
                const t = new Date(dateTimeNumber * 1000)
                const z = new Date(t.getTimezoneOffset() * 60 * 1000)
                const tLocal = new Date(t.getTime() - z.getTime())
                const tLocalDate = new Date(tLocal)
                let iso = tLocalDate.toISOString()
                iso = iso.split('.')[0]
                iso = iso.replace('T', ' ')
                return iso.slice(0, -3) // Removes :00 at the end
            },
            getFormattedFilter: (): MissionRunQueryParameters => {
                const localFilter = { ...filterState }
                // This way we avoid sending an empty filter which allows ongoing missions
                if (!localFilter.statuses || localFilter.statuses.length === 0)
                    localFilter.statuses = Object.assign([], missionStatusFilterOptionsIterable)
                return {
                    ...localFilter,
                    nameSearch: localFilter.missionName,
                    robotNameSearch: localFilter.robotName,
                    tagSearch: localFilter.tagId,
                }
            },
        }),
        [filterState, TranslateText]
    )

    useEffect(() => {
        const isAllNotSet = () => {
            if (Object.keys(filterState).length === 0) return true
            return Object.entries(filterState).every((entry) => filterFunctions.isSet(entry[0], entry[1]))
        }
        setFilterIsSet(!isAllNotSet())
    }, [filterState, filterFunctions])

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
