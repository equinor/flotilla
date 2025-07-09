import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot, RobotFlotillaStatus } from 'models/Robot'

const isBatteryTooLow = (robot: Robot): boolean => {
    if (robot.batteryLevel == null) return false
    if (
        (robot.model.batteryWarningThreshold && robot.batteryLevel < robot.model.batteryWarningThreshold) ||
        (robot.model.batteryMissionStartThreshold &&
            robot.batteryLevel < robot.model.batteryMissionStartThreshold &&
            robot.flotillaStatus === RobotFlotillaStatus.Recharging)
    ) {
        return true
    }
    return false
}

const isRobotPressureTooHigh = (robot: Robot): boolean => {
    if (robot.model.upperPressureWarningThreshold && robot.pressureLevel) {
        return robot.pressureLevel > robot.model.upperPressureWarningThreshold
    }
    return false
}

const isRobotPressureTooLow = (robot: Robot): boolean => {
    if (robot.model.lowerPressureWarningThreshold && robot.pressureLevel) {
        return robot.pressureLevel < robot.model.lowerPressureWarningThreshold
    }
    return false
}

export const NoMissionReason = ({ robot }: { robot: Robot }) => {
    const { TranslateText } = useLanguageContext()
    let message = undefined
    if (robot.flotillaStatus === RobotFlotillaStatus.Home) {
        message = TranslateText(
            'Robot is sent to dock and cannot run missions. Queued missions will run when robot is dismissed from dock.'
        )
    } else if (isBatteryTooLow(robot)) {
        message = robot.model.batteryMissionStartThreshold
            ? TranslateText(
                  'Battery is too low to start a mission. Queued missions will run when the battery is over {0}',
                  [robot.model.batteryMissionStartThreshold.toString()]
              ) + '%.'
            : TranslateText('Battery is too low to start a mission.')
    } else if (isRobotPressureTooHigh(robot)) {
        message = robot.model.upperPressureWarningThreshold
            ? TranslateText(
                  'Pressure is too high to start a mission. Queued missions will run when the pressure is under {0}mBar.',
                  [(robot.model.upperPressureWarningThreshold * 1000).toString()]
              )
            : TranslateText('Pressure is too high to start a mission.')
    } else if (isRobotPressureTooLow(robot)) {
        message = robot.model.lowerPressureWarningThreshold
            ? TranslateText(
                  'Pressure is too low to start a mission. Queued missions will run when the pressure is over {0}mBar.',
                  [(robot.model.lowerPressureWarningThreshold * 1000).toString()]
              )
            : TranslateText('Pressure is too low to start a mission.')
    }
    if (!message) {
        return <></>
    }
    return <Typography variant="body_short">{message}</Typography>
}
