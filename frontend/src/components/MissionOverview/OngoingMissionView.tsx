import { Typography } from '@equinor/eds-core-react'
import { defaultMission, Mission } from 'models/mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import { NoOngoingMissionsPlaceholder } from './NoMissionPlaceholder'
import { OngoingMissionCard } from './OngoingMissionCard'
var testMissions = [
    defaultMission['Pending'],
    defaultMission['Ongoing'],
    defaultMission['Warning'],
    defaultMission['Aborted'],
    defaultMission['Successful'],
    defaultMission['Paused'],
]

const StyledOngoingMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`
const OngoingMissionSection = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 2rem;
`

export function OngoingMissionView() {
    const [ongoingMissions, setOngoingMissions] = useState<Mission[]>([])
    useEffect(() => {
        setOngoingMissions(testMissions)
    }, [])
    console.log(ongoingMissions)
    var missionDisplay = ongoingMissions.map(function (mission, index) {
        return <OngoingMissionCard key={index} mission={mission} />
    })

    return (
        <StyledOngoingMissionView>
            <Typography variant="h2" color="resting">
                Ongoing Missions
            </Typography>
            <OngoingMissionSection>
                {ongoingMissions.length > 0 && missionDisplay}
                {ongoingMissions.length === 0 && <NoOngoingMissionsPlaceholder />}
            </OngoingMissionSection>
        </StyledOngoingMissionView>
    )
}
