import { Icon, Typography, Table } from '@equinor/eds-core-react'
import { info_circle } from '@equinor/eds-icons'
import styles from './activeMissions.module.css'
import MissionOverviewHeader from './ActiveMissionsHeader'
import { Mission } from 'models/mission'
import MissionRow from './ActiveMissionRow'
import { Robot } from 'models/robot'


Icon.add({ info_circle })

interface ActiveMissionsProps {
    missions: Mission[]
    robots: Robot[]
}

const ActiveMissions = ({ missions, robots }: ActiveMissionsProps): JSX.Element => {
    var rows = missions.map(function (mission) {
        return <MissionRow mission={mission} robots={robots} key={mission.name} />
    })
    return (
        <Table className={styles.tableWrapper}>
            <Table.Caption captionSide className={styles.tableCaption}>
                <Typography variant="h4">Active Missions</Typography>
            </Table.Caption>
            <MissionOverviewHeader />
            <Table.Body className={styles.tableBodyWrapper}>{rows}</Table.Body>
        </Table>
    )
}

export default ActiveMissions
