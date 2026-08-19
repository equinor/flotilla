import { Table, Typography, Icon, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionArea } from 'models/InspectionArea'
import { tokens } from '@equinor/eds-tokens'
import { MissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router'
import { Icons } from 'utils/icons'
import { compareMissionDefinitions } from './InspectionUtilities'
import { formatDateTime } from 'utils/StringFormatting'
import { useContext } from 'react'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { FrontPageSectionId } from 'models/FrontPageSectionId'
import { SmallScreenInfoText } from 'utils/InfoText'
import { phone_width } from 'utils/constants'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { StyledTableCell, StyledTableRow } from 'components/Styles/StyledComponents'

const StyledIcon = styled(Icon)`
    display: flex;
    justify-content: center;
    height: 1.3rem;
    width: 1.3rem;
`
const TableTitle = styled.p`
    margin: 0 0 0.875rem 0;
    font-family: Equinor, sans-serif;
    font-size: 0.92rem;
    font-weight: 600;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
`
const StyledTable = styled.div`
    display: grid;
    overflow-x: auto;
    @media (max-width: ${phone_width}) {
        width: calc(100vw - 30px);
    }
    max-width: fit-content;
`
const Centered = styled.div`
    display: flex;
    justify-content: center;
`

enum Columns {
    Name = 'Name',
    Description = 'Description',
    LastCompleted = 'LastCompleted',
    AddToQueue = 'AddToQueue',
}

const HideColumnsOnSmallScreen = styled.div`
    #SmallScreenInfoText {
        display: none;
    }
    @media (max-width: ${phone_width}) {
        #${Columns.Description} {
            display: none;
        }
        #${Columns.LastCompleted} {
            display: none;
        }
        #SmallScreenInfoText {
            display: grid;
            grid-template-columns: auto auto;
            gap: 0.3em;
            align-items: center;
            padding-bottom: 1rem;
        }
    }
`

interface IMissionRowProps {
    mission: MissionDefinition
    setMissions: (selectedMissions: MissionDefinition[]) => void
}

const MissionRow = ({ mission, setMissions }: IMissionRowProps) => {
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const navigate = useNavigate()

    const isScheduleButtonDisabled = enabledRobots.length === 0

    const lastCompleted = mission.lastSuccessfulRun?.endTime
        ? formatDateTime(mission.lastSuccessfulRun.endTime)
        : TranslateText('Never')

    return (
        <StyledTableRow key={mission.id}>
            <Table.Cell id={Columns.Name}>
                <Typography
                    link
                    onClick={() => navigate(`/${installation.installationCode}/missiondefinition/${mission.id}`)}
                >
                    {mission.name}
                </Typography>
            </Table.Cell>
            <Table.Cell id={Columns.Description} style={{ wordBreak: 'break-word' }}>
                {mission.comment}
            </Table.Cell>
            <Table.Cell id={Columns.LastCompleted}>{lastCompleted}</Table.Cell>
            <Table.Cell id={Columns.AddToQueue}>
                <Centered>
                    <Button
                        style={{ width: isScheduleButtonDisabled ? '110px' : '' }}
                        variant="ghost_icon"
                        disabled={isScheduleButtonDisabled}
                        onClick={() => {
                            setMissions([mission])
                        }}
                    >
                        <StyledIcon color={`${tokens.colors.interactive.focus.hex}`} name={Icons.Add} size={24} />
                        {isScheduleButtonDisabled && <>{TranslateText('No robot available')}</>}
                    </Button>
                </Centered>
            </Table.Cell>
        </StyledTableRow>
    )
}

interface IProps {
    inspectionArea: InspectionArea
    missionDefinitions: MissionDefinition[]
    setSelectedMissions: (selectedMissions: MissionDefinition[]) => void
}

export const MissionSchedulingTable = ({ inspectionArea, missionDefinitions, setSelectedMissions }: IProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledTable id={FrontPageSectionId.InspectionTable}>
            <HideColumnsOnSmallScreen>
                <Table>
                    <Table.Caption>
                        <TableTitle>{inspectionArea.inspectionAreaName}</TableTitle>
                        <SmallScreenInfoText />
                    </Table.Caption>
                    <Table.Head sticky>
                        <Table.Row>
                            {Object.values(Columns).map((col) => (
                                <StyledTableCell id={col} key={col}>
                                    {TranslateText(col)}
                                </StyledTableCell>
                            ))}
                        </Table.Row>
                    </Table.Head>
                    <Table.Body style={{ backgroundColor: tokens.colors.ui.background__default.hex }}>
                        {missionDefinitions.sort(compareMissionDefinitions).map((mission) => (
                            <MissionRow key={mission.id} mission={mission} setMissions={setSelectedMissions} />
                        ))}
                    </Table.Body>
                </Table>
            </HideColumnsOnSmallScreen>
        </StyledTable>
    )
}
