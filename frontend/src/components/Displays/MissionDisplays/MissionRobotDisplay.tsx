import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Mission } from 'models/Mission'
import { tokens } from '@equinor/eds-tokens'

const StyledRobotDisplay = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: space-between;
    overflow: hidden;
`
const EllipsisTypography = styled(Typography)`
    overflow: hidden;
    text-overflow: ellipsis;
`

interface MissionProps {
    mission: Mission
}

export const MissionRobotDisplay = ({ mission }: MissionProps) => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledRobotDisplay>
            <Typography variant="meta" color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText('Robot')}
            </Typography>
            <EllipsisTypography>{mission.robot.name}</EllipsisTypography>
        </StyledRobotDisplay>
    )
}
