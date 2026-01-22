import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotWithoutTelemetry, RobotStatus } from 'models/Robot'

export const NoMissionReason = ({ robot }: { robot: RobotWithoutTelemetry }) => {
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
