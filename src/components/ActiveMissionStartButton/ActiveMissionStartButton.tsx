import { Button, Icon } from '@equinor/eds-core-react'
import { play_circle, pause_circle_outlined, check_circle_outlined, close_circle_outlined, report } from '@equinor/eds-icons'
import { Mission, MissionStatus } from 'models/mission'
import { useRef, useState } from 'react'

interface ActiveMissionStartButtonProps {
    mission: Mission
}

Icon.add({ play_circle, pause_circle_outlined,check_circle_outlined, close_circle_outlined, report })


const ActiveMissionStartButton = ({ mission }: ActiveMissionStartButtonProps): JSX.Element => {
    const anchorRef = useRef<HTMLButtonElement>(null)
    const [check, setCheck] = useState(false); 

    let icon_name: string
    let icon_title: string
    if (mission.status === MissionStatus.InProgress){
        icon_name = "pause_circle_outlined"
        icon_title = "pause"
    }
    else if (mission.status === MissionStatus.Paused){
        icon_name = "play_circle"
        icon_title = "resume"
    }
    else if (mission.status === MissionStatus.Completed){
        icon_name = "check_circle_outlined"
        icon_title = "archive"
    }
    else if (mission.status === MissionStatus.Aborted){
        icon_name = "check_circel_outlined"
        icon_title = "archive"
    }
    else if (mission.status === MissionStatus.Error){
        icon_name = "close_circle_outlined"
        icon_title = "cancel mission"
    }
    else{
        icon_name = ""
        icon_title = ""
    }
        return (
        <>
            <Button ref={anchorRef} variant="ghost_icon" onClick={doAction()} title={icon_title}>
                <Icon name={icon_name} size={24} />
            </Button>
        </>
    )
}

export default ActiveMissionStartButton