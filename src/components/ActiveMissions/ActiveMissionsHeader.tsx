import { Table } from '@equinor/eds-core-react'
import styles from './activeMissions.module.css'

interface ActiveMissionsHeaderCellProps {
    label: string
    overideClassName?: string
}

const ActiveMissionsHeaderCell = ({ label, overideClassName }: ActiveMissionsHeaderCellProps): JSX.Element => {
    return (
        <Table.Cell className={overideClassName}>
            <span className={styles.label}>{label}</span>
        </Table.Cell>
    )
}

const ActiveMissionsHeader = (): JSX.Element => {
    return (
        <Table.Head className={styles.tableHeadWrapper}>
            <Table.Row className={styles.tableRowWrapper}>
                <ActiveMissionsHeaderCell label="Name" overideClassName={styles.tableNameCell} />
                <ActiveMissionsHeaderCell label="Robot" overideClassName={styles.tableRobotCell} />
                <ActiveMissionsHeaderCell label="Status" overideClassName={styles.tableStatusCell} />
                <ActiveMissionsHeaderCell label="Task" overideClassName={styles.tableTaskCell} />
                <ActiveMissionsHeaderCell label="" overideClassName={styles.tablePlayButtonCell}/>
            </Table.Row>
        </Table.Head>
    )
}

export default ActiveMissionsHeader
