import { CircularProgress, Pagination, Table, Typography } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import { HistoricMissionCard } from './HistoricMissionCard'
import { RefreshProps } from './MissionHistoryPage'
import styled from 'styled-components'
import { translateText } from 'components/Contexts/LanguageContext'
import { useErrorHandler } from 'react-error-boundary'
import { PaginationHeader } from 'models/PaginatedResponse'
import { BackendAPICaller } from 'api/ApiCaller'

const TableWithHeader = styled.div`
    gap: 2rem;
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
    const handleError = useErrorHandler()
    const pageSize: number = 10

    const completedStatuses = [
        MissionStatus.Aborted,
        MissionStatus.Cancelled,
        MissionStatus.Successful,
        MissionStatus.PartiallySuccessful,
        MissionStatus.Failed,
    ]
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
        BackendAPICaller.getMissions({ pageSize: pageSize, pageNumber: page, orderBy: 'EndTime desc, Name' }).then(
            (paginatedMissions) => {
                setPaginationDetails(paginatedMissions.pagination)
                setCompletedMissions(paginatedMissions.content.filter((m) => completedStatuses.includes(m.status)))
                setIsLoading(false)
            }
        )
        //.catch((e) => handleError(e))
    }

    var missionsDisplay = completedMissions.map(function (mission, index) {
        return <HistoricMissionCard key={index} index={index} mission={mission} />
    })

    const PaginationComponent = () => {
        return (
            <Pagination
                totalItems={paginationDetails!.TotalCount}
                itemsPerPage={paginationDetails!.PageSize}
                withItemIndicator
                onChange={(_, page) => onPageChange(page)}
            ></Pagination>
        )
    }

    const onPageChange = (page: number) => {
        setIsLoading(true)
        setCurrentPage(page)
    }

    return (
        <>
            <TableWithHeader>
                <Typography variant="h1">{translateText('Mission History')}</Typography>
                <Table>
                    <Table>
                        <Table.Head sticky>
                            <Table.Row>
                                <Table.Cell>{translateText('Status')}</Table.Cell>
                                <Table.Cell>{translateText('Name')}</Table.Cell>
                                <Table.Cell>{translateText('Robot')}</Table.Cell>
                                <Table.Cell>{translateText('Completion Time')}</Table.Cell>
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
                    </Table>
                    <Table.Caption captionSide={'bottom'}>
                        {paginationDetails && paginationDetails.TotalPages > 1 && PaginationComponent()}
                    </Table.Caption>
                </Table>
            </TableWithHeader>
        </>
    )
}
