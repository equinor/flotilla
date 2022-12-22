import { Typography } from "@equinor/eds-core-react"
import { MissionControlButtons } from "components/Pages/FrontPage/MissionOverview/MissionControlButtons"
import { Mission } from "models/Mission"
import styled from "styled-components"

const InfoSection = styled.div`
display: flex;
align-content: start;
gap: 1rem;
`

interface MissionHeaderProps {
    mission: Mission
}

export function MissionHeader({mission} : MissionHeaderProps){
    return(
        <InfoSection>
            <Typography variant="h1">{mission?.name}</Typography>
            <MissionControlButtons mission={mission} />
        </InfoSection>
    )
}