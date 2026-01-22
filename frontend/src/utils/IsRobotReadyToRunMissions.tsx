import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot, RobotStatus } from 'models/Robot'

export const NoMissionReason = ({ robot }: { robot: Robot }) => {
    const { TranslateText } = useLanguageContext()
    let message = undefined
    if (robot.status === RobotStatus.Lockdown || robot.status === RobotStatus.GoingToLockdown) {
        message = TranslateText(
            'Robot is sent to dock and cannot run missions. Queued missions will run when robot is dismissed from dock.'
        )
    }
    if (!message) {
        return <></>
    }
    return <Typography variant="body_short">{message}</Typography>
}
