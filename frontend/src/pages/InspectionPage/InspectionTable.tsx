import { Table, Typography, Icon, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { InspectionArea } from 'models/InspectionArea'
import { tokens } from '@equinor/eds-tokens'
import { MissionDefinition } from 'models/MissionDefinition'
import { useNavigate } from 'react-router-dom'
import { Icons } from 'utils/icons'
import { compareMissionDefinitions } from './InspectionUtilities'
import { formatDateTime } from 'utils/StringFormatting'
import { AlreadyScheduledMissionDialog, ScheduleMissionDialog } from './ScheduleMissionDialogs'
import { useContext, useEffect, useState } from 'react'
import { useMissionsContext } from 'components/Contexts/MissionRunsContext'
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

enum InspectionTableColumns {
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
        #${InspectionTableColumns.Description} {
            display: none;
        }
        #${InspectionTableColumns.LastCompleted} {
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

interface IProps {
    inspectionArea: InspectionArea
    missionDefinitions: MissionDefinition[]
    scrollOnToggle: boolean
    openDialog: () => void
    setSelectedMissions: (selectedMissions: MissionDefinition[]) => void
}

interface IMissionRowProps {
    mission: MissionDefinition
    openDialog: () => void
    setMissions: (selectedMissions: MissionDefinition[]) => void
    openScheduledDialog: () => void
}

const MissionRow = ({ mission, openDialog, setMissions, openScheduledDialog }: IMissionRowProps) => {
    const { TranslateText } = useLanguageContext()
    const { missionQueue } = useMissionsContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const navigate = useNavigate()

    const isScheduled = missionQueue.map((m) => m.missionId).includes(mission.id)
    const isScheduleButtonDisabled = enabledRobots.length === 0

    const lastCompleted = mission.lastSuccessfulRun?.endTime
        ? formatDateTime(mission.lastSuccessfulRun.endTime)
        : TranslateText('Never')

    const noRobotReadyForMissionsText = TranslateText('No robot available')

    return (
        <StyledTableRow key={mission.id}>
            <Table.Cell id={InspectionTableColumns.Name}>
                <Typography
                    link
                    onClick={() => navigate(`/${installation.installationCode}/missiondefinition/${mission.id}`)}
                >
                    {mission.name}
                </Typography>
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.Description} style={{ wordBreak: 'break-word' }}>
                {mission.comment}
            </Table.Cell>
            <Table.Cell id={InspectionTableColumns.LastCompleted}>{lastCompleted}</Table.Cell>
            <Table.Cell id={InspectionTableColumns.AddToQueue}>
                <Centered>
                    {!isScheduled && (
                        <Button
                            style={{ width: isScheduleButtonDisabled ? '110px' : '' }}
                            variant="ghost_icon"
                            disabled={isScheduleButtonDisabled}
                            onClick={() => {
                                openDialog()
                                setMissions([mission])
                            }}
                        >
                            <StyledIcon color={`${tokens.colors.interactive.focus.hex}`} name={Icons.Add} size={24} />
                            {isScheduleButtonDisabled && noRobotReadyForMissionsText}
                        </Button>
                    )}
                    {isScheduled && (
                        <Button
                            variant="ghost_icon"
                            disabled={enabledRobots.length === 0}
                            onClick={() => {
                                openScheduledDialog()
                                setMissions([mission])
                            }}
                        >
                            <StyledIcon color={`${tokens.colors.interactive.focus.hex}`} name={Icons.Add} size={24} />
                        </Button>
                    )}
                </Centered>
            </Table.Cell>
        </StyledTableRow>
    )
}

export const InspectionTable = ({ inspectionArea, missionDefinitions, openDialog, setSelectedMissions }: IProps) => {
    const { TranslateText } = useLanguageContext()

    const [isScheduledDialogOpen, setIsScheduledDialogOpen] = useState<boolean>(false)

    const openScheduleDialog = () => {
        setIsScheduledDialogOpen(true)
    }

    const closeScheduleDialog = () => {
        setIsScheduledDialogOpen(false)
    }

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
                            {Object.values(InspectionTableColumns).map((col) => (
                                <StyledTableCell id={col} key={col}>
                                    {TranslateText(col)}
                                </StyledTableCell>
                            ))}
                        </Table.Row>
                    </Table.Head>
                    <Table.Body style={{ backgroundColor: tokens.colors.ui.background__default.hex }}>
                        {missionDefinitions.sort(compareMissionDefinitions).map((mission) => (
                            <MissionRow
                                key={mission.id}
                                mission={mission}
                                openDialog={openDialog}
                                setMissions={setSelectedMissions}
                                openScheduledDialog={openScheduleDialog}
                            />
                        ))}
                    </Table.Body>
                </Table>
            </HideColumnsOnSmallScreen>
            {isScheduledDialogOpen && (
                <AlreadyScheduledMissionDialog openDialog={openDialog} closeDialog={closeScheduleDialog} />
            )}
        </StyledTable>
    )
}

interface Props {
    missionDefinitions: MissionDefinition[]
}

export const MissionDefinitionsTable = ({ missionDefinitions }: Props) => {
    const { TranslateText } = useLanguageContext()
    const { ongoingMissions, missionQueue } = useMissionsContext()
    const [selectedMissions, setSelectedMissions] = useState<MissionDefinition[]>()
    const [isDialogOpen, setIsDialogOpen] = useState<boolean>(false)
    const [isScheduledDialogOpen, setIsScheduledDialogOpen] = useState<boolean>(false)
    const [unscheduledMissions, setUnscheduledMissions] = useState<MissionDefinition[]>([])
    const [isAlreadyScheduled, setIsAlreadyScheduled] = useState<boolean>(false)

    const openDialog = () => {
        setIsDialogOpen(true)
    }

    const openScheduleDialog = () => {
        setIsScheduledDialogOpen(true)
    }

    const closeDialog = () => {
        setIsAlreadyScheduled(false)
        setSelectedMissions([])
        setUnscheduledMissions([])
        setIsDialogOpen(false)
    }

    const closeScheduleDialog = () => {
        setIsScheduledDialogOpen(false)
    }

    useEffect(() => {
        const isScheduled = (mission: MissionDefinition) => missionQueue.map((m) => m.missionId).includes(mission.id)
        const isOngoing = (mission: MissionDefinition) => ongoingMissions.map((m) => m.missionId).includes(mission.id)
        let unscheduledMissions: MissionDefinition[] = []
        if (selectedMissions) {
            selectedMissions.forEach((mission) => {
                if (isOngoing(mission) || isScheduled(mission)) setIsAlreadyScheduled(true)
                else unscheduledMissions = unscheduledMissions.concat([mission])
            })
            setUnscheduledMissions(unscheduledMissions)
        }
    }, [isDialogOpen, ongoingMissions, missionQueue, selectedMissions])

    return (
        <StyledTable>
            <HideColumnsOnSmallScreen>
                <Table>
                    <Table.Caption>
                        <SmallScreenInfoText />
                    </Table.Caption>
                    <Table.Head sticky>
                        <Table.Row>
                            {Object.values(InspectionTableColumns).map((col) => (
                                <StyledTableCell id={col} key={col}>
                                    {TranslateText(col)}
                                </StyledTableCell>
                            ))}
                        </Table.Row>
                    </Table.Head>
                    <Table.Body style={{ backgroundColor: tokens.colors.ui.background__default.hex }}>
                        {missionDefinitions.sort(compareMissionDefinitions).map((mission) => (
                            <MissionRow
                                key={mission.id}
                                mission={mission}
                                openDialog={openDialog}
                                setMissions={setSelectedMissions}
                                openScheduledDialog={openScheduleDialog}
                            />
                        ))}
                    </Table.Body>
                </Table>
                {isDialogOpen && (
                    <ScheduleMissionDialog
                        selectedMissions={selectedMissions!}
                        closeDialog={closeDialog}
                        setMissions={setSelectedMissions}
                        unscheduledMissions={unscheduledMissions}
                        isAlreadyScheduled={isAlreadyScheduled}
                    />
                )}
                {isScheduledDialogOpen && (
                    <AlreadyScheduledMissionDialog openDialog={openDialog} closeDialog={closeScheduleDialog} />
                )}
            </HideColumnsOnSmallScreen>
        </StyledTable>
    )
}
