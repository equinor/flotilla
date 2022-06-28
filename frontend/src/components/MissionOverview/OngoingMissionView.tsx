import { Typography } from '@equinor/eds-core-react'
import { defaultMission } from 'models/mission'
import styled from 'styled-components'
import { OngoingMissionCard } from './OngoingMissionCard'
const testMissions = [
    defaultMission['Pending'],
    defaultMission['Started'],
    defaultMission['Warning'],
    defaultMission['Failed'],
    defaultMission['Successful'],
]

const StyledOngoingMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    gap: 2rem;
`

export function OngoingMissionView() {
    var OngoingMissions = testMissions.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })
    return (
        <StyledOngoingMissionView>
            <Typography variant="h2" color="resting">
                {' '}
                Ongoing Missions
            </Typography>
            <OngoingMissionSection>{OngoingMissions}</OngoingMissionSection>
        </StyledOngoingMissionView>
    )
}
