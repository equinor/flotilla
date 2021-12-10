import { Icon, Typography, Table } from "@equinor/eds-core-react";
import { info_circle } from "@equinor/eds-icons";
import { Robot } from "../../models/robot";
import styles from "./robotOverview.module.css";
import RobotOverviewHeader from "./RobotOverviewHeader";

Icon.add({ info_circle });

interface RobotProps {
  robot: Robot;
}

const RobotStatus: React.FC<RobotProps> = ({ robot }: RobotProps) => {
  const name = robot.robotInfo.name;
  const type = robot.robotInfo.type;
  const status = robot.status;
  const battery = robot.battery;
  return (
    <Table.Row className={styles.tableRowWrapper}>
      <Table.Cell className={styles.tableNameCell}>{name}</Table.Cell>
      <Table.Cell className={styles.tableTypeCell}>{type}</Table.Cell>
      <Table.Cell className={styles.tableStatusCell}>{status}</Table.Cell>
      <Table.Cell className={styles.tableBatteryCell} variant="numeric">
        {battery}
      </Table.Cell>
      <Table.Cell className={styles.tableInfoCell} variant="icon">
        <Icon name="info_circle" size={24} color="primary" />
      </Table.Cell>
    </Table.Row>
  );
};

interface RobotOverviewProps {
  robots: Robot[];
}

const RobotOverview: React.FC<RobotOverviewProps> = ({
  robots,
}: RobotOverviewProps) => {
  var rows = robots.map(function (robot) {
    return <RobotStatus robot={robot} />;
  });
  return (
    <Table className={styles.tableWrapper}>
      <Table.Caption captionSide className={styles.tableCaption}>
        <Typography variant="h2">Robot Overview</Typography>
      </Table.Caption>
      <RobotOverviewHeader />
      <Table.Body className={styles.tableBodyWrapper}>
        {rows}
      </Table.Body>
    </Table>
  );
};

export default RobotOverview;
