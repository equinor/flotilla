import { Typography } from '@equinor/eds-core-react'
import { CondensedMissionDefinition } from 'models/MissionDefinition'
import { StyledDict } from '../MissionDefinitionStyledComponents'
interface MissionDefinitionHeaderProps {
    missionDefinition: CondensedMissionDefinition
}

export function MissionDefinitionHeader({ missionDefinition }: MissionDefinitionHeaderProps) {
    return (
        <StyledDict.HeaderSection>
            <StyledDict.TitleSection>
                <Typography variant="h1">{missionDefinition.name}</Typography>
            </StyledDict.TitleSection>
        </StyledDict.HeaderSection>
    )
}
