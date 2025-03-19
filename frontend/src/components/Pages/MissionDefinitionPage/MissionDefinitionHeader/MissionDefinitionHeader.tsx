import { Typography } from '@equinor/eds-core-react'
import { MissionDefinition } from 'models/MissionDefinition'
import { HeaderSection, TitleSection } from '../MissionDefinitionStyledComponents'
interface MissionDefinitionHeaderProps {
    missionDefinition: MissionDefinition
}

export const MissionDefinitionHeader = ({ missionDefinition }: MissionDefinitionHeaderProps) => {
    return (
        <HeaderSection>
            <TitleSection style={{ wordBreak: 'break-word' }}>
                <Typography variant="h1">{missionDefinition.name}</Typography>
            </TitleSection>
        </HeaderSection>
    )
}
