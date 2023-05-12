import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { MissionStatus } from 'models/Mission'
import styled from 'styled-components'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

interface StatusProps {
    status: MissionStatus
}

const StyledStatusDisplay = styled.div`
    display: flex;
    gap: 0.3em;
    align-items: flex-end;
`

export function displayIcon(status: MissionStatus) {
    switch (status) {
        case MissionStatus.Pending: {
            return <Icon name={Icons.Pending} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Ongoing: {
            return <Icon name={Icons.Ongoing} style={{ color: tokens.colors.text.static_icons__secondary.rgba }} />
        }
        case MissionStatus.Failed: {
            return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
        }
        case MissionStatus.Successful: {
            return <Icon name={Icons.Successful} style={{ color: tokens.colors.interactive.success__resting.rgba }} />
        }
        case MissionStatus.PartiallySuccessful: {
            return <Icon name={Icons.Warning} style={{ color: tokens.colors.interactive.warning__resting.rgba }} />
        }
    }
    return <Icon name={Icons.Failed} style={{ color: tokens.colors.interactive.danger__resting.rgba }} />
}

export function MissionStatusDisplay({ status }: StatusProps) {
    return (
        <StyledStatusDisplay>
            {displayIcon(status)}
            <Typography>{TranslateText(status)}</Typography>
        </StyledStatusDisplay>
    )
}
