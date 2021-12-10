import { Table } from "@equinor/eds-core-react";
import React from "react";
import styles from "./robotOverview.module.css";

interface RobotOverviewHeaderCellProps {
  label: string;
  overideClassName?: string;
}

const RobotOverviewHeaderCell: React.FC<RobotOverviewHeaderCellProps> = ({
  label,
  overideClassName,
}: RobotOverviewHeaderCellProps) => {
  return (
    <Table.Cell className={overideClassName}>
      <span className={styles.label}>{label}</span>
    </Table.Cell>
  );
};

const RobotOverviewHeader = () => {
  return (
    <Table.Head className={styles.tableHeadWrapper}>
      <Table.Row className={styles.tableRowWrapper}>
        <RobotOverviewHeaderCell
          label="Name"
          overideClassName={styles.tableNameCell}
        />
        <RobotOverviewHeaderCell
          label="Type"
          overideClassName={styles.tableTypeCell}
        />
        <RobotOverviewHeaderCell
          label="Status"
          overideClassName={styles.tableStatusCell}
        />
        <RobotOverviewHeaderCell
          label="Battery"
          overideClassName={styles.tableBatteryCell}
        />
        <RobotOverviewHeaderCell
          label="Info"
          overideClassName={styles.tableInfoCell}
        />
      </Table.Row>
    </Table.Head>
  );
};

export default RobotOverviewHeader;
