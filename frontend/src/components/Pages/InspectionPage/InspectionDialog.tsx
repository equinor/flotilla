import { Table, Card, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { useState, useEffect } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { Area } from 'models/Area'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { RefreshProps } from './InspectionPage'
import { tokens } from '@equinor/eds-tokens'
import { MissionDefinition } from 'models/MissionDefinition'

const StyledCard = styled(Card)`
    width: 200px;
    padding: 8px;
    :hover {
        background-color: #deedee;
    }
`

const StyledAreaCards = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
`

const TableWithHeader = styled.div`
    gap: 2rem;
`
const StyledContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 4rem;
`

interface AreaMissionType {
    [areaId: string]: { missionDefinitions: MissionDefinition[], area: Area }
}

export function AreasDialog({ refreshInterval }: RefreshProps) {
    const { TranslateText } = useLanguageContext()
    const { installationCode } = useInstallationContext()
    const [areaMissions, setAreaMissions] = useState<AreaMissionType>({})

    useEffect(() => {
        BackendAPICaller.getAreas().then(async (areas: Area[]) => {
            let newAreaMissions: AreaMissionType = {}
            const filteredAreas = areas.filter((area) => area.installationCode.toLowerCase() === installationCode.toLowerCase())
            for (const area of filteredAreas) {
                // These calls need to be made sequentially to update areaMissions safely
                let missionDefinitions = await BackendAPICaller.getMissionDefinitionsInArea(area)
                if (!missionDefinitions) missionDefinitions = []
                newAreaMissions[area.id] = { missionDefinitions: missionDefinitions, area: area }
            }
            setAreaMissions(newAreaMissions)
        })
    }, [installationCode])

    console.log(areaMissions)

    return (
        <>
            <StyledContent>
                <StyledAreaCards>
                    {Object.keys(areaMissions).map((areaId) => (
                        <StyledCard variant="default" key={areaId} style={{ boxShadow: tokens.elevation.raised }}>
                            <Typography>{areaMissions[areaId].area.areaName.toLocaleUpperCase()}</Typography>
                            <Typography>{
                                areaMissions[areaId] && areaMissions[areaId].missionDefinitions.length > 0 && 
                                    areaMissions[areaId].missionDefinitions[0].name}</Typography>
                        </StyledCard>
                    ))}
                </StyledAreaCards>
                <TableWithHeader>
                    <Typography variant="h1">{TranslateText('Area Inspections')}</Typography>
                    <Table>
                        <Table.Head sticky>
                            <Table.Row>
                                <Table.Cell>{TranslateText('Status')}</Table.Cell>
                                <Table.Cell>{TranslateText('Name')}</Table.Cell>
                                <Table.Cell>{TranslateText('Robot')}</Table.Cell>
                            </Table.Row>
                        </Table.Head>
                        <Table.Body>
                            {}
                        </Table.Body>
                    </Table>
                </TableWithHeader>
            </StyledContent>
        </>
    )
}
