import { Icon, Table } from '@equinor/eds-core-react'
import { Mission, Tag } from 'models/mission'
import styles from './missionOverview.module.css'
import { play_circle } from '@equinor/eds-icons'
import InfoButton from 'components/InfoButton/InfoButton'

Icon.add({ play_circle })

interface MissionRowProps {
    mission: Mission
}

const MissionRow = ({ mission }: MissionRowProps): JSX.Element => {
    const name: string = mission.name
    const link: string = mission.link
    const tags: Tag[] = mission.tags
    return (
        <Table.Row className={styles.tableRowWrapper}>
            <Table.Cell className={styles.tableNameCell}>{name}</Table.Cell>
            <Table.Cell className={styles.tableLinkCell}>
                <a target="_blank" href={link}>
                    {link}
                </a>
            </Table.Cell>
            <Table.Cell className={styles.tableInfoCell}>
                <InfoButton title="Missio Info" content={<div></div>}></InfoButton>
            </Table.Cell>
            <Table.Cell className={styles.tableStartMissionCell}>
                <Icon name="play_circle" size={24} color="primary" />
            </Table.Cell>
        </Table.Row>
    )
}

export default MissionRow
