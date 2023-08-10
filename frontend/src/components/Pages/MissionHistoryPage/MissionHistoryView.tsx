import { CircularProgress, Pagination, Table, Typography, Chip, Button, Dialog } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import { RefreshProps } from './MissionHistoryPage'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PaginationHeader } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'
import { useMissionFilterContext, IFilterState } from 'components/Contexts/MissionFilterContext'
import { FilterSection } from './FilterSection'
import { MissionStatus } from 'models/Mission'
import { InspectionType } from 'models/Inspection'
import { tokens } from '@equinor/eds-tokens'

const TableWithHeader = styled.div`
    display: flex;
    flex-direction: column;
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

function isNotNullOrNotEmptyArray(value: any | any[]) {
    if (typeof value === 'string' || typeof value === 'number') {
        return value
    } else if (Array.isArray(value)) {
        return value && value.length > 0
    }
}

function flatten(filters: IFilterState) {
    const allFilters = []
    for (const [filterName, filterValue] of Object.entries(filters)) {
        allFilters.push({ name: filterName, value: filterValue })
    }
    return allFilters
}

export function MissionHistoryView({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const pageSize: number = 10

    const [filteredMissions, setFilteredMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [isLoading, setIsLoading] = useState<boolean>(true)
    const [isResettingPage, setIsResettingPage] = useState<boolean>(false)

    const { page, switchPage, filterState, filterIsSet, filterFunctions, filterError, clearFilterError } =
        useMissionFilterContext()

    const FilterErrorDialog = () => {
        return (
            <>
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
            </>
        )
    }

    const checkBoxBackgroundColour = tokens.colors.ui.background__info.hex
    const checkBoxBorderColour = tokens.colors.interactive.primary__resting.hex

    const toDisplayValue = (filterName: string, value: string | number | MissionStatus[] | InspectionType[]) => {
        if (typeof value === 'string') {
            return value
        } else if (typeof value === 'number') {
            // We currently assume these are dates. We may want
            // to explicitly use the Date type in the filter context instead
            return filterFunctions.dateTimeIntToPrettyString(value)
        } else if (Array.isArray(value)) {
            let valueArray = value as any[]
            if (valueArray.length === 0) {
                console.error('Unexpected empty array detected')
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
        BackendAPICaller.getMissionRuns({
            ...filterState,
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
    }, [page, pageSize, filterState, switchPage])

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

    var missionsDisplay = filteredMissions.map(function (mission, index) {
        return <HistoricMissionCard key={index} index={index} mission={mission} />
    })

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
                        .filter((filter) => isNotNullOrNotEmptyArray(filter.value))
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
            <Table>
                <Table.Head sticky>
                    <Table.Row>
                        <Table.Cell>{TranslateText('Status')}</Table.Cell>
                        <Table.Cell>{TranslateText('Name')}</Table.Cell>
                        <Table.Cell>{TranslateText('Robot')}</Table.Cell>
                        <Table.Cell>{TranslateText('Completion Time')}</Table.Cell>
                    </Table.Row>
                </Table.Head>
                {isLoading && (
                    <Table.Caption captionSide={'bottom'}>
                        <StyledLoading>
                            <CircularProgress />
                        </StyledLoading>
                    </Table.Caption>
                )}
                {!isLoading && <Table.Body>{missionsDisplay}</Table.Body>}
                <Table.Caption captionSide={'bottom'}>
                    {paginationDetails && paginationDetails.TotalPages > 1 && !isResettingPage && PaginationComponent()}
                </Table.Caption>
            </Table>
        </TableWithHeader>
    )
}
