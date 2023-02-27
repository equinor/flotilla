import { Table, Typography } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { Mission, MissionStatus } from 'models/Mission'
import { useEffect, useState } from 'react'
import { PastMissionCard } from './PastMissionCard'
import { compareDesc } from 'date-fns'
import { RefreshProps } from '../FrontPage'
import styled from 'styled-components'

const TableWithHeader = styled.div`
    width: 600px;
    gap: 2rem;
`

const ScrollableTable = styled.div`
    display: grid;
    max-height: 250px;
    overflow: auto;
`

export function PastMissionView({ refreshInterval }: RefreshProps) {
    const completedStatuses = [
        MissionStatus.Aborted,
        MissionStatus.Cancelled,
        MissionStatus.Successful,
        MissionStatus.PartiallySuccessful,
        MissionStatus.Failed,
    ]
    const apiCaller = useApi()
    const [completedMissions, setCompletedMissions] = useState<Mission[]>([])

    useEffect(() => {
        const id = setInterval(() => {
            updateCompletedMissions()
        }, refreshInterval)
        return () => clearInterval(id)
    }, [])

    const updateCompletedMissions = () => {
        apiCaller.getMissions().then((missions) => {
            setCompletedMissions(
                missions
                    .filter((m) => completedStatuses.includes(m.missionStatus))
                    .sort((a, b) =>
                        compareDesc(
                            a.endTime === undefined ? new Date(0) : new Date(a.endTime),
                            b.endTime === undefined ? new Date(0) : new Date(b.endTime)
                        )
                    )
            )
        })
    }

    var missionsDisplay = completedMissions.map(function (mission, index) {
        return <PastMissionCard key={index} index={index} mission={mission} />
    })

    return (
        <TableWithHeader>
            <Typography variant="h2">Past Missions</Typography>
            <ScrollableTable>
                <Table>
                    <Table.Head sticky>
                        <Table.Row>
                            <Table.Cell>Status</Table.Cell>
                            <Table.Cell>Name</Table.Cell>
                            <Table.Cell>Completion Time</Table.Cell>
                        </Table.Row>
                    </Table.Head>
                    <Table.Body>{missionsDisplay}</Table.Body>
                </Table>
            </ScrollableTable>
        </TableWithHeader>
    )
}
