import { Icon, Typography, Table } from '@equinor/eds-core-react'
import { info_circle } from '@equinor/eds-icons'
import styles from './missionOverview.module.css'
import MissionOverviewHeader from './MissionOverviewHeader'
import { Mission } from 'models/mission'
import MissionRow from './MissionRow'
import { Robot } from 'models/robot'

Icon.add({ info_circle })

interface MissionOverviewProps {
    missions: Mission[]
    robots: Robot[]
}

const MissionOverview = ({ missions, robots }: MissionOverviewProps): JSX.Element => {
    var rows = missions.map(function (mission) {
        return <MissionRow mission={mission} robots={robots} key={mission.name} />
    })
    return (
        <Table className={styles.tableWrapper}>
            <Table.Caption captionSide className={styles.tableCaption}>
                <Typography variant="h2">Mission Overview</Typography>
            </Table.Caption>
            <MissionOverviewHeader />
            <Table.Body className={styles.tableBodyWrapper}>{rows}</Table.Body>
        </Table>
    )
}

export default MissionOverview
