import { CircularProgress, Table } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/Mission'
import { useCallback, useEffect, useState } from 'react'
import { SimpleHistoricMissionCard } from './HistoricMissionCard'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { PaginationHeader } from 'models/PaginatedResponse'
import { useMissionFilterContext, MissionFilterProvider } from 'components/Contexts/MissionFilterContext'
import { StyledTableBody, StyledTableCaption, StyledTableCell } from 'components/Styles/StyledComponents'
import { SignalREventLabels, useSignalRContext } from 'components/Contexts/SignalRContext'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { StyledLoading, StyledPagination } from './MissionHistoryView'
import { useBackendApi } from 'api/UseBackendApi'

enum InspectionTableColumns {
    Status = 'Status',
    Name = 'Name',
    CompletionTime = 'CompletionTime',
}

const TableWithHeader = styled.div`
    width: 100%;
    padding: 1rem;
    display: grid;
    grid-columns: auto;
    gap: 1.5rem;
`
const StyledTable = styled.div`
    display: grid;
    overflow-x: auto;
    overflow-y: hidden;
`

export const DataViewPage = () => (
    <MissionFilterProvider>
        <DataViewComponent />
    </MissionFilterProvider>
)

const DataViewComponent = () => {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useAssetContext()
    const { page, switchPage, filterState, filterFunctions } = useMissionFilterContext()
    const { registerEvent, connectionReady } = useSignalRContext()
    const [filteredMissions, setFilteredMissions] = useState<Mission[]>([])
    const [paginationDetails, setPaginationDetails] = useState<PaginationHeader>()
    const [isLoading, setIsLoading] = useState<boolean>(true)
    const [isResettingPage, setIsResettingPage] = useState<boolean>(false)
    const [lastChangedMission, setLastChangedMission] = useState<Mission | undefined>(undefined)
    const pageSize: number = 20
    const backendApi = useBackendApi()

    const updateFilteredMissions = useCallback(() => {
        const formattedFilter = filterFunctions.getFormattedFilter()
        backendApi
            .getMissionRuns({
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
            .catch(() => {})
    }, [page, pageSize, filterFunctions])

    useEffect(() => {
        if (isResettingPage) setIsResettingPage(false)
    }, [isResettingPage])

    useEffect(() => {
        updateFilteredMissions()
    }, [page, filterState])

    useEffect(() => {
        if (
            lastChangedMission &&
            lastChangedMission.installationCode === installationCode &&
            ![MissionStatus.Pending, MissionStatus.Queued, MissionStatus.Ongoing].includes(lastChangedMission.status)
        ) {
            updateFilteredMissions()
        }
    }, [lastChangedMission])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunCreated, (username: string, message: string) => {
                setLastChangedMission(JSON.parse(message))
            })
            registerEvent(SignalREventLabels.missionRunUpdated, (username: string, message: string) => {
                setLastChangedMission(JSON.parse(message))
            })
            registerEvent(SignalREventLabels.missionRunDeleted, (username: string, message: string) => {
                setLastChangedMission(JSON.parse(message))
            })
        }
    }, [registerEvent, connectionReady])

    const missionsDisplay = filteredMissions.map((mission, index) => (
        <SimpleHistoricMissionCard key={index} index={index} mission={mission} />
    ))

    const PaginationComponent = () => (
        <StyledPagination
            totalItems={paginationDetails!.TotalCount}
            itemsPerPage={paginationDetails!.PageSize}
            withItemIndicator
            defaultPage={page}
            onChange={(_, newPage) => onPageChange(newPage)}
        ></StyledPagination>
    )

    const onPageChange = (newPage: number) => {
        setIsLoading(true)
        switchPage(newPage)
    }

    return (
        <TableWithHeader>
            <StyledTable>
                <Table>
                    {isLoading && (
                        <StyledTableCaption captionSide={'bottom'}>
                            <StyledLoading>
                                <CircularProgress />
                            </StyledLoading>
                        </StyledTableCaption>
                    )}
                    <StyledTableCaption captionSide={'bottom'}>
                        {paginationDetails && paginationDetails.TotalPages > 1 && !isResettingPage && (
                            <PaginationComponent />
                        )}
                    </StyledTableCaption>
                    <Table.Head sticky>
                        <Table.Row>
                            <StyledTableCell id={InspectionTableColumns.Status}>
                                {TranslateText('Status')}
                            </StyledTableCell>
                            <StyledTableCell id={InspectionTableColumns.Name}>{TranslateText('Name')}</StyledTableCell>
                            <StyledTableCell id={InspectionTableColumns.CompletionTime}>
                                {TranslateText('Completion Time')}
                            </StyledTableCell>
                        </Table.Row>
                    </Table.Head>
                    {!isLoading && <StyledTableBody>{missionsDisplay}</StyledTableBody>}
                </Table>
            </StyledTable>
        </TableWithHeader>
    )
}
