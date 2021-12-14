import { Icon, Typography, Table } from '@equinor/eds-core-react'
import { info_circle } from '@equinor/eds-icons'
import { Robot, RobotStatus } from 'models/robot'
import RobotInfoButton from './RobotInfoButton'
import styles from './robotOverview.module.css'
import RobotOverviewHeader from './RobotOverviewHeader'

import RobotStatusView from './RobotStatusView'

Icon.add({ info_circle })

interface RobotProps {
    robot: Robot
}

const RobotStatusRow = ({ robot }: RobotProps): JSX.Element => {
    const name: string = robot.robotInfo.name
    const type: string = robot.robotInfo.type
    const status: RobotStatus = robot.status
    const battery: number = robot.battery
    return (
        <Table.Row className={styles.tableRowWrapper}>
            <Table.Cell className={styles.tableNameCell}>{name}</Table.Cell>
            <Table.Cell className={styles.tableTypeCell}>{type}</Table.Cell>
            <Table.Cell className={styles.tableStatusCell}>
                <div className={styles.tableStatusCellDiv}>
                    <RobotStatusView status={status} />
                </div>
            </Table.Cell>
            <Table.Cell className={styles.tableBatteryCell} variant="numeric">
                {battery}
            </Table.Cell>
            <Table.Cell className={styles.tableInfoCell}>
                <RobotInfoButton robot={robot} />
            </Table.Cell>
        </Table.Row>
    )
}

interface RobotOverviewProps {
    robots: Robot[]
}

const RobotOverview = ({ robots }: RobotOverviewProps): JSX.Element => {
    var rows = robots.map(function (robot) {
        return <RobotStatusRow robot={robot} />
    })
    return (
        <Table className={styles.tableWrapper}>
            <Table.Caption captionSide className={styles.tableCaption}>
                <Typography variant="h2">Robot Overview</Typography>
            </Table.Caption>
            <RobotOverviewHeader />
            <Table.Body className={styles.tableBodyWrapper}>{rows}</Table.Body>
        </Table>
    )
}

export default RobotOverview
