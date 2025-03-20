import { Icon, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { TextAlignedButton } from 'components/Styles/StyledComponents'
import { AlertListContents } from './AlertsListItem'

const StyledDiv = styled.div`
    align-items: center;
`
const StyledAlertTitle = styled.div`
    display: flex;
    gap: 0.5em;
    align-items: flex-end;
`
const Indent = styled.div`
    padding: 5px 9px;
`

export const InfoAlertContent = ({ title, message }: { title: string; message: string }) => {
    const iconColor = tokens.colors.interactive.primary__resting.hex

    return (
        <StyledDiv>
            <StyledAlertTitle>
                <Icon name={Icons.Info} style={{ color: iconColor }} />
                <Typography>{title}</Typography>
            </StyledAlertTitle>
            <Indent>
                <TextAlignedButton variant="ghost" color="secondary">
                    {message}
                </TextAlignedButton>
            </Indent>
        </StyledDiv>
    )
}

export const InfoAlertListContent = ({ title, message }: { title: string; message: string }) => {
    return (
        <AlertListContents
            icon={Icons.Info}
            iconColor={tokens.colors.interactive.primary__resting.hex}
            alertTitle={title}
            alertText={message}
        />
    )
}
