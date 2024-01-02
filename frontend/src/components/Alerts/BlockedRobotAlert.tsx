import { Button, Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { Robot } from 'models/Robot'

const StyledDiv = styled.div`
    align-items: center;
`

const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

const Indent = styled.div`
    padding: 0px 9px;
`

interface AlertProps {
    robot: Robot
}

export const BlockedRobotAlertContent = ({ robot }: AlertProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
                <Typography>{TranslateText('Robot is blocked')}</Typography>
            </StyledAlertTitle>
            <Indent>
                <Button as={Typography} variant="ghost" color="secondary">
                    {TranslateText(`The robot '${robot.name}' is blocked and cannot perform tasks.`)}
                </Button>
            </Indent>
        </StyledDiv>
    )
}
