import { Robot } from 'models/Robot'
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
    missionDeckNames: string[]
}

export const ScheduleMissionWithConfirmDialogs = ({
    robotId,
    missionDeckNames,
    scheduleMissions,
    closeDialog,
}: ConfirmScheduleDialogProps) => {
    const { enabledRobots } = useRobotContext()
    const [robot, setRobot] = useState<Robot>()

    const isBatteryInsufficient = (currentRobot: Robot) =>
        currentRobot.batteryLevel &&
        currentRobot.model.batteryWarningThreshold &&
        currentRobot.batteryLevel < currentRobot.model.batteryWarningThreshold

    const isPressureInsufficient = (currentRobot: Robot) =>
        currentRobot.pressureLevel &&
        ((currentRobot.model.lowerPressureWarningThreshold &&
            currentRobot.pressureLevel < currentRobot.model.lowerPressureWarningThreshold) ||
            (currentRobot.model.upperPressureWarningThreshold &&
                currentRobot.pressureLevel > currentRobot.model.upperPressureWarningThreshold))

    useEffect(() => {
        setRobot(enabledRobots.find((robot) => robot.id === robotId))
    }, [robotId, enabledRobots])

    if (!robot) {
        return <></>
    } else if (isBatteryInsufficient(robot)) {
        return <InsufficientBatteryDialog robot={robot} cancel={closeDialog} />
    } else if (isPressureInsufficient(robot)) {
        return <InsufficientPressureDialog robot={robot} cancel={closeDialog} />
    } else {
        return (
            <ScheduleMissionWithLocalizationVerificationDialog
                scheduleMissions={scheduleMissions}
                closeDialog={closeDialog}
                robotId={robot!.id}
                missionDeckNames={missionDeckNames}
            />
        )
    }
}
