import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { TextAlignedButton } from 'components/Styles/StyledComponents'

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
    robotNames: string[]
}

export const BlockedRobotAlertContent = ({ robotNames }: AlertProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
                <Typography>{TranslateText('Robot is blocked')}</Typography>
            </StyledAlertTitle>
            <Indent>
                <TextAlignedButton variant="ghost" color="secondary">
                    {robotNames.length === 1 &&
                        `${TranslateText('The robot')} ${robotNames[0]} ${TranslateText(
                            'is blocked and cannot perform tasks'
                        )}.`}
                    {robotNames.length > 1 && TranslateText('Several robots are blocked and cannot perform tasks.')}
                </TextAlignedButton>
            </Indent>
        </StyledDiv>
    )
}
