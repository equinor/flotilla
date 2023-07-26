import { CircularProgress, Pagination, Table, Typography } from '@equinor/eds-core-react'
import { Mission } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import { RefreshProps } from './MissionHistoryPage'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PaginationHeader } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'
import { useMissionFilterContext } from 'components/Contexts/MissionFilterContext'
import { FilterSection } from './FilterSection'

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

export function MissionHistoryView({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const pageSize: number = 10

    const [filteredMissions, setFilteredMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [isLoading, setIsLoading] = useState<boolean>(true)
    const [isResettingPage, setIsResettingPage] = useState<boolean>(false)

    const {
        page,
        switchPage,
        filterState
    } = useMissionFilterContext()

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
