import { Button, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { UpcomingMissionCard } from './UpcomingMissionCard'
import { useApi, useInterval } from 'api/ApiCaller'
import { useContext, useEffect, useState } from 'react'
import { Mission } from 'models/Mission'
import { NoUpcomingMissionsPlaceholder } from './NoMissionPlaceholder'
import { ScheduleMissionDialog } from './ScheduleMissionDialog'

const StyledMissionView = styled.div`
    display: grid;
    grid-column: 1/ -1;
    gap: 1rem;
`

const MissionTable = styled.div`
    display: grid;
    grid-template-rows: repeat(auto-fill);
    gap: 1rem;
`

const MissionButtonView = styled.div`
    display: flex;
    gap: 2rem;
`
const processEchoMissions = (missions: Mission[]): string[] => {
    const stringifiedArray: string[] = [];
    missions.map((mission: Mission) => {
        stringifiedArray.push(mission.id + ": " + mission.name);
    })
    return stringifiedArray;
}

const mapNameToIdentifier = (name: string | null): string => {
    //replace with fetching names and identifiers(as Enum?)
    const namesAndIdentifiers: Array<Array<string>> = [["Kårstø", "kaa"], ["Test", "test"], ["Johan Sverdrup", "jsv"]];
    var installationCode: string = ""
    namesAndIdentifiers.map((nameAndIdentifier: Array<string>) => {
        if (nameAndIdentifier[0] == name) {
            installationCode = nameAndIdentifier[1];
        }
    })
    return installationCode
}

export function MissionView() {
    const apiCaller = useApi()
    const [upcomingMissions, setUpcomingMissions] = useState<Mission[]>([])
    const [echoMissions, setEchoMissions] = useState<Mission[]>([]);
    useEffect(() => {
        apiCaller.getUpcomingMissions().then((missions) => {
            setUpcomingMissions(missions)
        })
    }, [])
    useEffect(() => {
        const installationName = sessionStorage.getItem('assetString');
        const installationCode = mapNameToIdentifier(installationName);
        apiCaller.getEchoMissionsForPlant(installationCode).then((missions) => {
            setEchoMissions(missions)
        })
    }, [sessionStorage.getItem('assetString')])
    useInterval(async () => {
        apiCaller.getUpcomingMissions().then((missions) => {
            setUpcomingMissions(missions)
        })
    })

    var upcomingMissionDisplay = upcomingMissions.map(function (mission, index) {
        return <UpcomingMissionCard key={index} mission={mission} />
    })
    return (
        <StyledMissionView>
            <Typography variant="h2" color="resting">
                Upcoming missions
            </Typography>
            <MissionTable>
                {upcomingMissions.length > 0 && upcomingMissionDisplay}
                {upcomingMissions.length === 0 && <NoUpcomingMissionsPlaceholder />}
            </MissionTable>
            <MissionButtonView>
                <ScheduleMissionDialog options={processEchoMissions(echoMissions)}></ScheduleMissionDialog>
                <Button>Make new mission in Echo</Button>
            </MissionButtonView>
        </StyledMissionView>
    )
}
