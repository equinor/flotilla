import { CircularProgress, Pagination, Table, Typography, Chip, Button, Dialog } from '@equinor/eds-core-react'
import { Mission, MissionStatusFilterOptions } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PaginationHeader } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'
import { useMissionFilterContext, IFilterState } from 'components/Contexts/MissionFilterContext'
import { FilterSection } from './FilterSection'
import { InspectionType } from 'models/Inspection'
import { tokens } from '@equinor/eds-tokens'
import { SmallScreenInfoText } from 'utils/InfoText'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { StyledTableBody, StyledTableCaption, StyledTableCell } from 'components/Styles/StyledComponents'
import { phone_width } from 'utils/constants'

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
    @media (max-width: ${phone_width}) {
        #SmallScreenInfoText {
            display: grid;
            grid-template-columns: auto auto;
            gap: 0.3em;
            align-items: left;
            padding-bottom: 1rem;
            max-width: 400px;
        }
    }
    @media (max-width: ${phone_width}) {
        #${InspectionTableColumns.Status} {
            display: none;
        }
        #${InspectionTableColumns.Robot} {
            display: none;
        }
        #${InspectionTableColumns.CompletionTime} {
            display: none;
        }
    }
    @media (min-width: ${phone_width}) ) {
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
    padding-left: var(--page-margin);
    padding-right: var(--page-margin);
    flex-wrap: wrap;
    min-height: 24px;
`
const StyledTable = styled.div`
    display: grid;
    overflow-x: auto;
    overflow-y: hidden;
`
const StyledPagination = styled(Pagination)`
    display: flex;
    height: 48px;
    padding: 0px 8px 0px 16px;
    background-color: ${tokens.colors.ui.background__default.hex};
`
const StyledActiveFilterList = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 10px;
`

const flatten = (filters: IFilterState) => {
    const allFilters = []
    for (const [filterName, filterValue] of Object.entries(filters)) {
        allFilters.push({ name: filterName, value: filterValue })
    }
    return allFilters
}

type RefreshProps = {
    refreshInterval: number
}

export const MissionHistoryView = ({ refreshInterval }: RefreshProps) => {
    const { TranslateText } = useLanguageContext()
    const { page, switchPage, filterState, filterIsSet, filterFunctions, filterError, clearFilterError } =
        useMissionFilterContext()
    const { setAlert, setListAlert } = useAlertContext()
    const [filteredMissions, setFilteredMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [isLoading, setIsLoading] = useState<boolean>(true)
    const [isResettingPage, setIsResettingPage] = useState<boolean>(false)
    const pageSize: number = 10
    const checkBoxBackgroundColour = tokens.colors.ui.background__info.hex
    const checkBoxBorderColour = tokens.colors.ui.background__info.hex
    const checkBoxWhiteBackgroundColor = tokens.colors.ui.background__default.hex

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
        value: boolean | string | number | MissionStatusFilterOptions[] | InspectionType[]
    ) => {
        if (typeof value === 'boolean') {
            return ''
        } else if (typeof value === 'string') {
            return value
        } else if (typeof value === 'number') {
            // We currently assume these are dates. We may want
            // to explicitly use the Date type in the filter context instead
            return filterFunctions.dateTimeIntToPrettyString(value)
        } else if (Array.isArray(value)) {
            const valueArray = value as any[]
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
        })
            .then((paginatedMissions) => {
                setFilteredMissions(paginatedMissions.content)
                setPaginationDetails(paginatedMissions.pagination)
                if (page > paginatedMissions.pagination.TotalPages && paginatedMissions.pagination.TotalPages > 0) {
                    switchPage(paginatedMissions.pagination.TotalPages)
                    setIsResettingPage(true)
                }
                setIsLoading(false)
            })
            .catch(() => {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent
                        translatedMessage={TranslateText('Failed to retrieve previous mission runs')}
                    />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent
                        translatedMessage={TranslateText('Failed to retrieve previous mission runs')}
                    />,
                    AlertCategory.ERROR
                )
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
            <StyledPagination
                totalItems={paginationDetails!.TotalCount}
                itemsPerPage={paginationDetails!.PageSize}
                withItemIndicator
                defaultPage={page}
                onChange={(_, newPage) => onPageChange(newPage)}
            ></StyledPagination>
        )
    }

    const onPageChange = (newPage: number) => {
        setIsLoading(true)
        switchPage(newPage)
    }

    const ActiveFilterContent = () => {
        return flatten(filterState)
            .filter((filter) => !filterFunctions.isSet(filter.name, filter.value))
            .map((filter) => {
                const valueToDisplay = toDisplayValue(filter.name, filter.value!)
                const filterName = valueToDisplay ? `${TranslateText(filter.name)}: ` : TranslateText(filter.name)
                return (
                    <Chip
                        style={{
                            backgroundColor: checkBoxWhiteBackgroundColor,
                            borderColor: checkBoxBorderColour,
                            height: '2rem',
                            paddingLeft: '10px',
                        }}
                        key={filter.name}
                        onDelete={() => filterFunctions.removeFilter(filter.name)}
                    >
                        {filterName} {valueToDisplay}
                    </Chip>
                )
            })
    }

    return (
        <TableWithHeader>
            <FilterSection />
            {filterIsSet && (
                <StyledActiveFilterList>
                    <Typography variant="caption" color="gray">
                        {TranslateText('Active Filters')}
                        {':'}
                    </Typography>
                    <ActiveFilterList>
                        <ActiveFilterContent />
                    </ActiveFilterList>
                </StyledActiveFilterList>
            )}
            {filterError && <FilterErrorDialog />}
            <StyledTable>
                <HideColumnsOnSmallScreen>
                    <SmallScreenInfoText />
                    <Table>
                        {isLoading && (
                            <StyledTableCaption captionSide={'bottom'}>
                                <StyledLoading>
                                    <CircularProgress />
                                </StyledLoading>
                            </StyledTableCaption>
                        )}
                        <StyledTableCaption captionSide={'bottom'}>
                            {paginationDetails &&
                                paginationDetails.TotalPages > 1 &&
                                !isResettingPage &&
                                PaginationComponent()}
                        </StyledTableCaption>
                        <Table.Head sticky>
                            <Table.Row>
                                <StyledTableCell id={InspectionTableColumns.StatusShort}>
                                    {TranslateText('Status')}
                                </StyledTableCell>
                                <StyledTableCell id={InspectionTableColumns.Status}>
                                    {TranslateText('Status')}
                                </StyledTableCell>
                                <StyledTableCell id={InspectionTableColumns.Name}>
                                    {TranslateText('Name')}
                                </StyledTableCell>
                                <StyledTableCell id={InspectionTableColumns.Robot}>
                                    {TranslateText('Robot')}
                                </StyledTableCell>
                                <StyledTableCell id={InspectionTableColumns.CompletionTime}>
                                    {TranslateText('Completion Time')}
                                </StyledTableCell>
                                <StyledTableCell id={InspectionTableColumns.Rerun}>
                                    {TranslateText('Add to queue')}
                                </StyledTableCell>
                            </Table.Row>
                        </Table.Head>
                        {!isLoading && <StyledTableBody>{missionsDisplay}</StyledTableBody>}
                    </Table>
                </HideColumnsOnSmallScreen>
            </StyledTable>
        </TableWithHeader>
    )
}
