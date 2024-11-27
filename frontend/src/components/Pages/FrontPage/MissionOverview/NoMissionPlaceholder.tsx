import { Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { tokens } from '@equinor/eds-tokens'

const StyledPlaceholder = styled(Card)`
    display: flex;
    box-sizing: border-box;
    padding: 24px;
    min-width: 250px;
    border: 1px solid ${tokens.colors.interactive.disabled__border.hex};
    background-color: ${tokens.colors.ui.background__light.hex};
`

export const EmptyMissionQueuePlaceholder = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledPlaceholder>
            <Typography variant="h4" color="disabled">
                {' '}
                {TranslateText('No missions in queue')}{' '}
            </Typography>
        </StyledPlaceholder>
    )
}
