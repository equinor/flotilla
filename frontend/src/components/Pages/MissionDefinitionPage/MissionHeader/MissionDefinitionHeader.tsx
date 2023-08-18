import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MissionDefinition } from 'models/MissionDefinition'

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
    missionDefinition: MissionDefinition
}

export function MissionDefinitionHeader({ missionDefinition }: MissionDefinitionHeaderProps) {
    const { TranslateText } = useLanguageContext()

    return (
        <HeaderSection>
            <TitleSection>
                <Typography variant="h1">{missionDefinition.name}</Typography>
            </TitleSection>
            <Typography
                variant="body_long_italic"
                group="paragraph"
                color={tokens.colors.text.static_icons__secondary.rgba}
            >
                {missionDefinition.comment}
            </Typography>
        </HeaderSection>
    )
}
