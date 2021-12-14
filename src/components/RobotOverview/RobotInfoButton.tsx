import { Typography } from '@equinor/eds-core-react'
import { Pose } from 'models/pose'
import { Robot } from 'models/robot'
import InfoButton from '../InfoButton/InfoButton'
import styles from './robotOverview.module.css'

interface RobotInfoButtonProps {
    robot: Robot
}

interface RobotPoseInfoProps {
    pose: Pose
}

const RobotPoseInfo = ({ pose }: RobotPoseInfoProps): JSX.Element => {
    return (
        <div className={styles.infoContentWrapper}>
            <div className={styles.poseWrapper}>
                <Typography variant="body_short_bold">Position:</Typography>
                <div className={styles.poseItem}>
                    <Typography>X: {pose.position.x}</Typography>
                    <Typography>Y: {pose.position.x}</Typography>
                    <Typography>Z: {pose.position.x}</Typography>
                </div>
                <Typography variant="body_short_bold">Orientation:</Typography>
                <div className={styles.poseItem}>
                    <Typography>X: {pose.orientation.x}</Typography>
                    <Typography>Y: {pose.orientation.y}</Typography>
                    <Typography>Z: {pose.orientation.z}</Typography>
                    <Typography>W: {pose.orientation.w}</Typography>
                </div>
            </div>
        </div>
    )
}

const RobotInfoButton = ({ robot }: RobotInfoButtonProps): JSX.Element => {
    const content = <RobotPoseInfo pose={robot.pose} />
    return <InfoButton title="Robot Info" content={content}></InfoButton>
}

export default RobotInfoButton
