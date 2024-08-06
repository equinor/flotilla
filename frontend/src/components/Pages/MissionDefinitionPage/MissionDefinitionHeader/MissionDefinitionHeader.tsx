import { Typography } from '@equinor/eds-core-react'
import { MissionDefinition } from 'models/MissionDefinition'
import { StyledDict } from 'components/Pages/MissionDefinitionPage/MissionDefinitionStyledComponents'
interface MissionDefinitionHeaderProps {
    missionDefinition: MissionDefinition
}

export const MissionDefinitionHeader = ({ missionDefinition }: MissionDefinitionHeaderProps) => {
    return (
        <StyledDict.HeaderSection>
            <StyledDict.TitleSection style={{ wordBreak: 'break-word' }}>
                <Typography variant="h1">{missionDefinition.name}</Typography>
            </StyledDict.TitleSection>
        </StyledDict.HeaderSection>
    )
}
