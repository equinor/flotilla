import { Button, Dialog, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Robot } from 'models/Robot'
import {
    StyledDialog,
    VerticalContent,
} from 'components/Displays/ConfirmScheduleDialogs/LocalizationVerification/ScheduleMissionStyles'

enum InsufficientDialogTypes {
    battery = 'battery',
    pressure = 'pressure',
}

interface SpecificInsufficientDialogProps {
    robot: Robot
    cancel: () => void
}

interface GenericInsufficientDialogProps {
    robot: Robot
    dialogType: InsufficientDialogTypes
    value: number
    lowerLimit?: number
    upperLimit?: number
    cancel: () => void
}

export const InsufficientBatteryDialog = ({ robot, cancel }: SpecificInsufficientDialogProps) => {
    return (
        <InsufficientDialog
            robot={robot}
            dialogType={InsufficientDialogTypes.battery}
            value={robot.batteryLevel!}
            lowerLimit={robot.model.batteryWarningThreshold}
            cancel={cancel}
        />
    )
}

export const InsufficientPressureDialog = ({ robot, cancel }: SpecificInsufficientDialogProps) => {
    return (
        <InsufficientDialog
            robot={robot}
            dialogType={InsufficientDialogTypes.pressure}
            value={robot.pressureLevel!}
            lowerLimit={robot.model.lowerPressureWarningThreshold}
            upperLimit={robot.model.upperPressureWarningThreshold}
            cancel={cancel}
        />
    )
}

const InsufficientDialog = ({
    robot,
    dialogType,
    value,
    lowerLimit,
    upperLimit,
    cancel,
}: GenericInsufficientDialogProps) => {
    const { TranslateText } = useLanguageContext()

    const getValueWithUnit = (value: number): string => {
        if (dialogType === InsufficientDialogTypes.battery) {
            return `${Math.round(value)}%`
        } else if (dialogType === InsufficientDialogTypes.pressure) {
            const barToMillibar = 1000
            return `${Math.round(value * barToMillibar)}mBar`
        } else return value.toString()
    }

    const getAction = (): string => {
        if (dialogType === InsufficientDialogTypes.battery) return 'charge'
        else if (dialogType === InsufficientDialogTypes.pressure)
            return lowerLimit && value < lowerLimit ? 'pressurize' : 'de-pressurize'
        else return ''
    }

    let warningText = `${TranslateText(`Current ${dialogType} value for`)} ${robot.name} (${
        robot.model.type
    }) ${TranslateText(`is`)} ${getValueWithUnit(value)}. `

    if (lowerLimit && value < lowerLimit) {
        warningText += `${TranslateText('This is below recommended lower limit of')} ${getValueWithUnit(lowerLimit)}.`
    } else if (upperLimit && value > upperLimit) {
        warningText += `${TranslateText('This is above recommended upper limit of')} ${getValueWithUnit(upperLimit)}.`
    }

    const actionText = `${TranslateText(`Please ${getAction()} the robot`)}.`

    return (
        <StyledDialog open={true} onClose={cancel}>
            <Dialog.Header>
                <Typography variant="h5">{TranslateText(`${dialogType} warning`)}</Typography>
            </Dialog.Header>
            <Dialog.Content>
                <VerticalContent>
                    <Typography>{warningText}</Typography>
                    <Typography>{actionText}</Typography>
                    <Dialog.Actions>
                        <Button variant="outlined" onClick={cancel}>
                            {TranslateText('Close')}
                        </Button>
                    </Dialog.Actions>
                </VerticalContent>
            </Dialog.Content>
        </StyledDialog>
    )
}
