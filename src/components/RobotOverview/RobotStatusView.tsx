import React from 'react';
import { Typography } from '@equinor/eds-core-react'
import { RobotStatus } from 'models/robot'
import styles from './robotOverview.module.css'

export interface RobotStatusViewProps {
    status: RobotStatus
}

const RobotStatusView = ({ status }: RobotStatusViewProps): JSX.Element => {
    let style_background
    if (status === RobotStatus.Available) {
        style_background = styles.available
    } else if (status === RobotStatus.MissionInProgress) {
        style_background = styles.missionInProgress
    } else {
        style_background = styles.offline
    }
    return (
        <div className={style_background}>
            <Typography style={{ textAlign: 'center' }}>{status}</Typography>
        </div>
    )
}

export default RobotStatusView
