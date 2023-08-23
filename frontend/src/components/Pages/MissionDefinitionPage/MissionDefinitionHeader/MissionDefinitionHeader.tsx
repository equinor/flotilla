import { Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { CondensedMissionDefinition, MissionDefinition } from 'models/MissionDefinition'

const HeaderSection = styled.div`
    display: flex;
    flex-direction: column;
    gap: 0.4rem;
`
const TitleSection = styled.div`
    display: flex;
    align-items: center;
    gap: 10px;
`

interface MissionDefinitionHeaderProps {
    missionDefinition: CondensedMissionDefinition
}

export function MissionDefinitionHeader({ missionDefinition }: MissionDefinitionHeaderProps) {
    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{missionDefinition.name}</Typography>
            </TitleSection>
        </HeaderSection>
    )
}
