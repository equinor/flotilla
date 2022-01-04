import { Icon, Table } from '@equinor/eds-core-react'
import { Mission, MissionStatus } from 'models/mission'
import styles from './activeMissions.module.css'
import { play_circle } from '@equinor/eds-icons'
import { Robot } from 'models/robot'
import ActiveMissionStatusView from './ActiveMissionStatusView'
import ActiveMissionStartButton from '../ActiveMissionStartButton/ActiveMissionStartButton'


Icon.add({ play_circle })

interface ActiveMissionRowProps {
    mission: Mission
    robots: Robot[]
}

const ActiveMissionRow = ({ mission, robots }: ActiveMissionRowProps): JSX.Element => {
    const mission_name: string = mission.name
    const robot_name: string = robots[0].robotInfo.name
    const mission_status: MissionStatus = mission.status
    const mission_task: string = mission.task_of_total_task_string()

    return (
        <Table.Row className={styles.tableRowWrapper}>
            <Table.Cell className={styles.tableNameCell}>{mission_name}</Table.Cell>
            <Table.Cell className={styles.tableRobotCell}>{robot_name}</Table.Cell>
            <Table.Cell className={styles.tableStatusCell}>
                <div className={styles.tableStatusColorBar}>
                    <ActiveMissionStatusView status={mission_status} />
                </div>
            </Table.Cell>
            <Table.Cell className={styles.tableTaskCell}>{mission_task}</Table.Cell>
            <Table.Cell className={styles.tablePlayButtonCell}>
                <ActiveMissionStartButton mission={mission} />
            </Table.Cell>  
        </Table.Row>
        
    )
}


export default ActiveMissionRow
