import { CircularProgress, Pagination, Table, Typography, Chip, Button, Dialog } from '@equinor/eds-core-react'
import { Mission, MissionStatusFilterOptions } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import { RefreshProps } from './MissionHistoryPage'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PaginationHeader } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'
import { useMissionFilterContext, IFilterState } from 'components/Contexts/MissionFilterContext'
import { FilterSection } from './FilterSection'
import { InspectionType } from 'models/Inspection'
import { tokens } from '@equinor/eds-tokens'
import { SmallScreenInfoText } from 'utils/InfoText'

enum InspectionTableColumns {
    StatusShort = 'StatusShort',
    Status = 'Status',
    Name = 'Name',
    Area = 'Area',
    Robot = 'Robot',
    CompletionTime = 'CompletionTime',
    Rerun = 'RerunMission',
}

const HideColumnsOnSmallScreen = styled.div`
    #SmallScreenInfoText {
        display: none;
    }
    @media (max-width: 730px) {
        #SmallScreenInfoText {
            display: grid;
            grid-template-columns: auto auto;
            gap: 0.3em;
            align-items: left;
            padding-bottom: 1rem;
            max-width: 400px;
        }
    }
    @media (max-width: 600px) {
        #${InspectionTableColumns.Status} {
            display: none;
        }
        #${InspectionTableColumns.Area} {
            display: none;
        }
        #${InspectionTableColumns.Robot} {
            display: none;
        }
        #${InspectionTableColumns.CompletionTime} {
            display: none;
        }
    }
    @media (min-width: 601px) {
        #${InspectionTableColumns.StatusShort} {
            display: none;
        }
    }
`

const TableWithHeader = styled.div`
    display: grid;
    grid-columns: auto;
    gap: 1rem;
`
const StyledLoading = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 1rem;
    padding-bottom: 1rem;
    gap: 1rem;
`

const ActiveFilterList = styled.div`
    display: flex;
    gap: 0.7rem;
    align-items: center;
    margin-left: var(--page-margin);
    margin-right: var(--page-margin);
    margin-top: 8px;
    flex-wrap: wrap;
    min-height: 24px;
`

const StyledTable = styled.div`
    display: grid;
    overflow-x: auto;
    overflow-y: hidden;
`

const flatten = (filters: IFilterState) => {
    const allFilters = []
    for (const [filterName, filterValue] of Object.entries(filters)) {
        allFilters.push({ name: filterName, value: filterValue })
    }
    return allFilters
}

export const MissionHistoryView = ({ refreshInterval }: RefreshProps) => {
    const { TranslateText } = useLanguageContext()
    const { page, switchPage, filterState, filterIsSet, filterFunctions, filterError, clearFilterError } =
        useMissionFilterContext()
    const [filteredMissions, setFilteredMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [isLoading, setIsLoading] = useState<boolean>(true)
    const [isResettingPage, setIsResettingPage] = useState<boolean>(false)
    const pageSize: number = 10
    const checkBoxBackgroundColour = tokens.colors.ui.background__info.hex
    const checkBoxBorderColour = tokens.colors.interactive.primary__resting.hex

    const FilterErrorDialog = () => {
        return (
            <Dialog open={filterError !== ''} isDismissable onClose={() => clearFilterError()}>
                <Dialog.Header>
                    <Dialog.Title>{TranslateText('Filter error')}</Dialog.Title>
                </Dialog.Header>
                <Dialog.CustomContent>
                    <Typography variant="body_short">{filterError}</Typography>
                </Dialog.CustomContent>
                <Dialog.Actions>
                    <Button onClick={() => clearFilterError()}>{TranslateText('Close')}</Button>
                </Dialog.Actions>
            </Dialog>
        )
    }

    const toDisplayValue = (
        filterName: string,
        value: string | number | MissionStatusFilterOptions[] | InspectionType[]
    ) => {
        if (typeof value === 'string') {
            return value
        } else if (typeof value === 'number') {
            // We currently assume these are dates. We may want
            // to explicitly use the Date type in the filter context instead
            return filterFunctions.dateTimeIntToPrettyString(value)
        } else if (Array.isArray(value)) {
            let valueArray = value as any[]
            if (valueArray.length === 0) {
                return <>{TranslateText('None')}</>
            }
            return valueArray.map((val) => (
                <Chip
                    style={{ background: checkBoxBackgroundColour, borderColor: checkBoxBorderColour }}
                    key={filterName + val}
                    onDelete={() => filterFunctions.removeFilterElement(filterName, val)}
                >
                    {TranslateText(val)}
                </Chip>
            ))
        } else {
            console.error('Unexpected filter type detected')
        }
    }

    const updateFilteredMissions = useCallback(() => {
        const formattedFilter = filterFunctions.getFormattedFilter()!
        BackendAPICaller.getMissionRuns({
            ...formattedFilter,
            pageSize: pageSize,
            pageNumber: page ?? 1,
            orderBy: 'EndTime desc, Name',
        }).then((paginatedMissions) => {
            setFilteredMissions(paginatedMissions.content)
            setPaginationDetails(paginatedMissions.pagination)
            if (page > paginatedMissions.pagination.TotalPages && paginatedMissions.pagination.TotalPages > 0) {
                switchPage(paginatedMissions.pagination.TotalPages)
                setIsResettingPage(true)
            }
            setIsLoading(false)
        })
    }, [page, pageSize, switchPage, filterFunctions])

    useEffect(() => {
        if (isResettingPage) setIsResettingPage(false)
    }, [isResettingPage])

    useEffect(() => {
        updateFilteredMissions()
        const id = setInterval(() => {
            updateFilteredMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [refreshInterval, updateFilteredMissions, page])

    const missionsDisplay = filteredMissions.map((mission, index) => (
        <HistoricMissionCard key={index} index={index} mission={mission} />
    ))

    const PaginationComponent = () => {
        return (
            <Pagination
                totalItems={paginationDetails!.TotalCount}
                itemsPerPage={paginationDetails!.PageSize}
                withItemIndicator
                defaultPage={page}
                onChange={(_, newPage) => onPageChange(newPage)}
            ></Pagination>
        )
    }

    const onPageChange = (newPage: number) => {
        setIsLoading(true)
        switchPage(newPage)
    }

    return (
        <TableWithHeader>
            <Typography variant="h1">{TranslateText('Mission History')}</Typography>
            <FilterSection />
            {filterIsSet && (
                <ActiveFilterList>
                    {flatten(filterState)
                        .filter((filter) => !filterFunctions.isSet(filter.name, filter.value))
                        .map((filter) => (
                            <Chip
                                style={{ borderColor: checkBoxBorderColour, height: '2rem', paddingLeft: '6px' }}
                                key={filter.name}
                                onDelete={() => filterFunctions.removeFilter(filter.name)}
                            >
                                {TranslateText(filter.name)}: {toDisplayValue(filter.name, filter.value!)}
                            </Chip>
                        ))}
                </ActiveFilterList>
            )}
            {filterError && <FilterErrorDialog />}
            <StyledTable>
                <HideColumnsOnSmallScreen>
                    <SmallScreenInfoText />
                    <Table>
                        {isLoading && (
                            <Table.Caption captionSide={'bottom'}>
                                <StyledLoading>
                                    <CircularProgress />
                                </StyledLoading>
                            </Table.Caption>
                        )}
                        <Table.Caption captionSide={'bottom'}>
                            {paginationDetails &&
                                paginationDetails.TotalPages > 1 &&
                                !isResettingPage &&
                                PaginationComponent()}
                        </Table.Caption>
                        <Table.Head sticky>
                            <Table.Row>
                                <Table.Cell id={InspectionTableColumns.StatusShort}>
                                    {TranslateText('Status')}
                                </Table.Cell>
                                <Table.Cell id={InspectionTableColumns.Status}>{TranslateText('Status')}</Table.Cell>
                                <Table.Cell id={InspectionTableColumns.Name}>{TranslateText('Name')}</Table.Cell>
                                <Table.Cell id={InspectionTableColumns.Area}>{TranslateText('Area')}</Table.Cell>
                                <Table.Cell id={InspectionTableColumns.Robot}>{TranslateText('Robot')}</Table.Cell>
                                <Table.Cell id={InspectionTableColumns.CompletionTime}>
                                    {TranslateText('Completion Time')}
                                </Table.Cell>
                                <Table.Cell id={InspectionTableColumns.Rerun}>
                                    {TranslateText('Add to queue')}
                                </Table.Cell>
                            </Table.Row>
                        </Table.Head>
                        {!isLoading && <Table.Body>{missionsDisplay}</Table.Body>}
                    </Table>
                </HideColumnsOnSmallScreen>
            </StyledTable>
        </TableWithHeader>
    )
}
