import { Table } from '@equinor/eds-core-react'
import styles from './missionOverview.module.css'

interface MissionOverviewHeaderCellProps {
    label: string
    overideClassName?: string
}

const MissionOverviewHeaderCell = ({ label, overideClassName }: MissionOverviewHeaderCellProps): JSX.Element => {
    return (
        <Table.Cell className={overideClassName}>
            <span className={styles.label}>{label}</span>
        </Table.Cell>
    )
}

const MissionOverviewHeader = (): JSX.Element => {
    return (
        <Table.Head className={styles.tableHeadWrapper}>
            <Table.Row className={styles.tableRowWrapper}>
                <MissionOverviewHeaderCell label="Name" overideClassName={styles.tableNameCell} />
                <MissionOverviewHeaderCell label="Link" overideClassName={styles.tableLinkCell} />
                <MissionOverviewHeaderCell label="Info" overideClassName={styles.tableInfoCell} />
                <MissionOverviewHeaderCell label="Start Mission" overideClassName={styles.tableStartMissionCell} />
            </Table.Row>
        </Table.Head>
    )
}

export default MissionOverviewHeader
