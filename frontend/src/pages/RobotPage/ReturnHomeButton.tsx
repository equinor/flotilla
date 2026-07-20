import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { RobotWithoutTelemetry } from 'models/Robot'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { Button } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useState } from 'react'
import { useBackendApi } from 'api/UseBackendApi'

const StyledTextButton = styled(Button)`
    height: auto;
    min-height: ${tokens.shape.button.minHeight};
    text-align: left;
    align-self: stretch;
`

export const ReturnHomeButton = ({ robot }: { robot: RobotWithoutTelemetry }) => {
    const { TranslateText } = useLanguageContext()
    const { raiseAlert } = useAlertContext()
    const [isDisabled, setIsDisabled] = useState(false)
    const backendApi = useBackendApi()

    const returnRobotToHome = () => {
        disableButton()

        backendApi.returnRobotToHome(robot.id).catch(() => {
            raiseAlert(AlertType.RequestFail, {
                kind: 'requestFail',
                message: TranslateText('Failed to send robot {0} home', [robot.name ?? '']),
            })
        })
    }

    const disableButton = () => {
        setIsDisabled(true)
        setTimeout(() => {
            setIsDisabled(false)
        }, 1000)
    }

    return (
        <StyledTextButton variant="outlined" disabled={isDisabled} onClick={returnRobotToHome}>
            {TranslateText('Return robot to home')}
        </StyledTextButton>
    )
}
