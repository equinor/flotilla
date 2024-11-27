import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Mission } from 'models/Mission'
import styled from 'styled-components'

const StyledAreaDisplay = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: space-between;
`

export const MissionAreaDisplay = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledAreaDisplay>
            <Typography variant="meta" color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText('Area')}
            </Typography>
            <Typography>{mission.area?.areaName || 'N/A'}</Typography>
        </StyledAreaDisplay>
    )
}
