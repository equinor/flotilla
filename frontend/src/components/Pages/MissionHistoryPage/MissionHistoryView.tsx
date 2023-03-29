import { CircularProgress, Pagination, Table, Typography } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import { compareDesc } from 'date-fns'
import { RefreshProps } from './MissionHistoryPage'
import styled from 'styled-components'
import { Text } from 'components/Contexts/LanguageContext'
import { useErrorHandler } from 'react-error-boundary'
import { compareByDate } from 'utils/filtersAndSorts'
import { PaginationHeader } from 'models/PaginatedResponse'

const TableWithHeader = styled.div`
    width: 600px;
    gap: 2rem;
`
const StyledLoading = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding-top: 3rem;
    gap: 1rem;
`

export function MissionHistoryView({ refreshInterval }: RefreshProps) {
    const handleError = useErrorHandler()
    const pageSize: number = 20

    const completedStatuses = [
        MissionStatus.Aborted,
        MissionStatus.Cancelled,
        MissionStatus.Successful,
        MissionStatus.PartiallySuccessful,
        MissionStatus.Failed,
    ]
    const apiCaller = useApi()
    const [completedMissions, setCompletedMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [currentPage, setCurrentPage] = useState<number>()
    const [isLoading, setIsLoading] = useState<boolean>(true)

    useEffect(() => {
        const id = setInterval(() => {
            updateCompletedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [currentPage])

    const updateCompletedMissions = () => {
        const page = currentPage ?? 1
        console.log('Page is ' + page)
        apiCaller.getMissions({ pageSize: pageSize, pageNumber: page }).then((paginatedMissions) => {
            setPaginationDetails(paginatedMissions.pagination)
            setCompletedMissions(
                paginatedMissions.content
                    .filter((m) => completedStatuses.includes(m.status))
                    .sort((a, b) => compareByDate(a.endTime, b.endTime))
            )
            setIsLoading(false)
        })
        //.catch((e) => handleError(e))
    }

    var missionsDisplay = completedMissions.map(function (mission, index) {
        return <HistoricMissionCard key={index} index={index} mission={mission} />
    })

    const onPageChange = (page: number) => {
        console.log('Changing page to ' + page)
        setIsLoading(true)
        setCurrentPage(page)
    }

    return (
        <>
            <TableWithHeader>
                <Typography variant="h1">{Text('Mission History')}</Typography>
                {isLoading && (
                    <StyledLoading>
                        <CircularProgress />
                    </StyledLoading>
                )}
                {!isLoading && (
                    <Table>
                        <Table.Head sticky>
                            <Table.Row>
                                <Table.Cell>{Text('Status')}</Table.Cell>
                                <Table.Cell>{Text('Name')}</Table.Cell>
                                <Table.Cell>{Text('Completion Time')}</Table.Cell>
                            </Table.Row>
                        </Table.Head>
                        <Table.Body>{missionsDisplay}</Table.Body>
                    </Table>
                )}
                {paginationDetails && paginationDetails.TotalPages > 1 && (
                    <Pagination
                        totalItems={paginationDetails!.TotalCount}
                        itemsPerPage={paginationDetails!.PageSize}
                        withItemIndicator
                        onChange={(_, page) => onPageChange(page)}
                    />
                )}
            </TableWithHeader>
        </>
    )
}
