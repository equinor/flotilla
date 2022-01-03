import { Icon, Table } from '@equinor/eds-core-react'
import { Mission } from 'models/mission'
import styles from './missionOverview.module.css'
import { play_circle } from '@equinor/eds-icons'
import InfoButton from 'components/InfoButton/InfoButton'
import StartMissionButton from 'components/StartMissionButton/StartMissionButton'
import { Robot } from 'models/robot'

Icon.add({ play_circle })

interface MissionRowProps {
    mission: Mission
    robots: Robot[]
}

const MissionRow = ({ mission, robots }: MissionRowProps): JSX.Element => {
    const name: string = mission.name
    const link: string = mission.link
    return (
        <Table.Row className={styles.tableRowWrapper}>
            <Table.Cell className={styles.tableNameCell}>{name}</Table.Cell>
            <Table.Cell className={styles.tableLinkCell}>
                <a target="_blank" rel="noreferrer" href={link}>
                    {link}
                </a>
            </Table.Cell>
            <Table.Cell className={styles.tableInfoCell}>
                <InfoButton title="Mission Info" content={<div></div>}></InfoButton>
            </Table.Cell>
            <Table.Cell className={styles.tableStartMissionCell}>
                <StartMissionButton robots={robots} mission={mission} />
            </Table.Cell>
        </Table.Row>
    )
}

export default MissionRow
