import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { AttributeTitleTypography } from 'components/Styles/StyledComponents'
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
            <AttributeTitleTypography>{TranslateText('Area')}</AttributeTitleTypography>
            <Typography>{mission.area?.areaName || 'N/A'}</Typography>
        </StyledAreaDisplay>
    )
}
