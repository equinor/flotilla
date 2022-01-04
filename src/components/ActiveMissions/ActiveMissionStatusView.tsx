import { Typography } from '@equinor/eds-core-react'
import styles from './activeMissions.module.css'
import { MissionStatus } from 'models/mission'

export interface ActiveMissionStatusViewProps {
    status: MissionStatus
}

const ActiveMissionStatusView = ({ status }: ActiveMissionStatusViewProps): JSX.Element => {
    let style_background
    if (status === MissionStatus.InProgress) {
        style_background = styles.active
    } 
    else if (status === MissionStatus.Completed) {
        style_background = styles.completed
    }
    else if (status === MissionStatus.Paused) {
        style_background = styles.paused
    }
    else if (status === MissionStatus.Aborted){
        style_background = styles.aborted
    }  
    else {
        style_background = styles.error
    }
    return (
        <div className={style_background}>
            <Typography style={{ textAlign: 'center' }}>{status}</Typography>
        </div>
    )
}

export default ActiveMissionStatusView
