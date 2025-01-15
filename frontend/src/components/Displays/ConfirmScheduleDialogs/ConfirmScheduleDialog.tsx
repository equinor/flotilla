import { Robot, RobotCapabilitiesEnum } from 'models/Robot'
import { useEffect, useState } from 'react'
import { useRobotContext } from 'components/Contexts/RobotContext'
import {
    InsufficientBatteryDialog,
    InsufficientPressureDialog,
} from 'components/Displays/ConfirmScheduleDialogs/InsufficientValueDialogs'
import { ScheduleMissionWithLocalizationVerificationDialog } from './LocalizationVerification/ScheduleMissionWithLocalizationVerification'
import { isBatteryTooLow, isRobotPressureTooHigh, isRobotPressureTooLow } from 'utils/IsRobotReadyToRunMissions'

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

    useEffect(() => {
        setRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    useEffect(() => {
        if (shouldScheduleWithoutLocalization) {
            scheduleMissions()
            closeDialog()
        }
    }, [shouldScheduleWithoutLocalization])

    if (!robot) {
        return <></>
    } else if (isBatteryTooLow(robot)) {
        return <InsufficientBatteryDialog robot={robot} cancel={closeDialog} />
    } else if (isRobotPressureTooLow(robot) || isRobotPressureTooHigh(robot)) {
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
