import { Robot, RobotCapabilitiesEnum } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useRobotContext } from 'components/Contexts/RobotContext'
import {
    InsufficientBatteryDialog,
    InsufficientPressureDialog,
} from 'components/Displays/ConfirmScheduleDialogs/InsufficientValueDialogs'
import { ScheduleMissionWithLocalizationVerificationDialog } from './LocalizationVerification/ScheduleMissionWithLocalizationVerification'

interface ConfirmScheduleDialogProps {
    scheduleMissions: () => void
    closeDialog: () => void
    robotId: string
    missionInspectionAreaNames: string[]
}

export const ScheduleMissionWithConfirmDialogs = ({
    robotId,
    missionInspectionAreaNames,
    scheduleMissions,
    closeDialog,
}: ConfirmScheduleDialogProps) => {
    const { enabledRobots } = useRobotContext()
    const [robot, setRobot] = useState<Robot>()
    const [shouldScheduleWithoutLocalization, setShouldScheduleWithoutLocalization] = useState<boolean>()

    const isBatteryInsufficient = (currentRobot: Robot) => {
        const hasBatteryValue = currentRobot.batteryLevel !== null && currentRobot.batteryLevel !== undefined
        return (
            hasBatteryValue &&
            currentRobot.model.batteryWarningThreshold &&
            currentRobot.batteryLevel! < currentRobot.model.batteryWarningThreshold
        )
    }

    const isPressureInsufficient = (currentRobot: Robot) => {
        const hasPressureValue = currentRobot.pressureLevel !== null && currentRobot.pressureLevel !== undefined
        return (
            (hasPressureValue &&
                currentRobot.model.lowerPressureWarningThreshold &&
                currentRobot.pressureLevel! < currentRobot.model.lowerPressureWarningThreshold) ||
            (currentRobot.model.upperPressureWarningThreshold &&
                currentRobot.pressureLevel! > currentRobot.model.upperPressureWarningThreshold)
        )
    }

    useEffect(() => {
        setRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (shouldScheduleWithoutLocalization) {
            scheduleMissions()
            closeDialog()
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [shouldScheduleWithoutLocalization])

    if (!robot) {
        return <></>
    } else if (isBatteryInsufficient(robot)) {
        return <InsufficientBatteryDialog robot={robot} cancel={closeDialog} />
    } else if (isPressureInsufficient(robot)) {
        return <InsufficientPressureDialog robot={robot} cancel={closeDialog} />
    } else {
        // Auto-localizing robots don't need to confirmation localization. Localization dialog can be skipped
        if (
            robot.robotCapabilities?.includes(RobotCapabilitiesEnum.auto_localize) &&
            !shouldScheduleWithoutLocalization
        ) {
            setShouldScheduleWithoutLocalization(true)
            return <></>
        }
        return (
            <ScheduleMissionWithLocalizationVerificationDialog
                scheduleMissions={scheduleMissions}
                closeDialog={closeDialog}
                robotId={robot!.id}
                missionInspectionAreaNames={missionInspectionAreaNames}
            />
        )
    }
}
