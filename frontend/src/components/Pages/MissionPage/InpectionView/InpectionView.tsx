import { Button, Card, Dialog, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useCallback, useEffect, useRef, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { BackendAPICaller } from 'api/ApiCaller'
import { Task, TaskStatus } from 'models/Task'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'

const StyledInspection = styled.canvas`
    flex: 1 0 0;
    align-self: stretch;
    max-height: 60vh;
    width: 80vh;
`

const StyledInspectionImage = styled.canvas`
    flex: 1 0 0;
    align-self: center;
    max-width: 100%;
`

const StyledDialog = styled(Dialog)`
    display: flex;
    width: 100%;
    max-height: 80vh;
`
const StyledCloseButton = styled(Button)`
    width: 24px;
    height: 24px;
`
const StyledDialogContent = styled(Dialog.Content)`
    display: flex;
    flex-direction: column;
    gap: 10px;
`
const StyledDialogHeader = styled.div`
    display: flex;
    padding: 16px;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
    border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    height: 24px;
`

const StyledBottomContent = styled.div`
    display: flex;
    padding: 16px;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
`

const StyledInfoContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`

const StyledSection = styled.div`
    display: flex;
    padding: 24px;
    min-width: 240px;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    border-radius: 6px;
    border: 1.194px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
    overflow-y: scroll;
    max-height: 60vh;
`

const StyledImagesSection = styled.div`
    display: flex;
    align-items: center;
    gap: 16px;
`

const StyledImageCard = styled(Card)`
    display: flex;
    max-height: 280px;
    max-width: 210px;
    align-self: stretch;
    padding: 4px;
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
    flex: auto;
    border-radius: 2px;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
    background: ${tokens.colors.ui.background__default.hex};
    box-shadow:
        0px 2.389px 4.778px 0px ${tokens.colors.ui.background__light.hex},
        0px 3.583px 4.778px 0px ${tokens.colors.ui.background__light.hex};
    cursor: pointer;
`

const StyledInspectionData = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    gap: 8px;
`
const StyledInspectionContent = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-start;
`
const StyledDialogInspectionView = styled.div`
    display: flex;
    flex-direction: row;
    gap: 16px;
`

const StyledInspectionCards = styled.div`
    display: flex;
    justify-content: center;
    align-items: flex-start;
    align-content: flex-start;
    gap: 8px;
    align-self: stretch;
    flex-wrap: wrap;
`

interface InspectionDialogViewProps {
    task: Task
    setInspectionTask: (inspectionTask: Task | undefined) => void
    tasks: Task[]
}

export const getMeta = async (url: string) => {
    const image = new Image()
    image.src = url
    await image.decode()
    return image
}

export const InspectionDialogView = ({ task, setInspectionTask, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationCode, installationName } = useInstallationContext()
    const [inspectionCanvas, setInspectionCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [inspectionImage, setInspectionImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [inspectionContext, setInspectionContext] = useState<CanvasRenderingContext2D>()
    const imageObjectURL = useRef<string>('')

    let taskId = task.isarTaskId!

    useEffect(() => {
        BackendAPICaller.getInspection(installationCode, taskId)
            .then((imageBlob) => {
                imageObjectURL.current = URL.createObjectURL(imageBlob)
            })
            .then(() => {
                getMeta(imageObjectURL.current).then((img) => {
                    const inspectionCanvas = document.getElementById('inspectionCanvas') as HTMLCanvasElement
                    if (inspectionCanvas) {
                        inspectionCanvas.width = img.width
                        inspectionCanvas.height = img.height
                        let context = inspectionCanvas.getContext('2d')
                        if (context) {
                            setInspectionContext(context)
                            context.drawImage(img, 0, 0)
                        }
                        setInspectionCanvas(inspectionCanvas)
                    }
                    setInspectionImage(img)
                })
            })
            .catch((e) => {})
    }, [installationCode, taskId, task])

    return (
        <>
        {imageObjectURL.current != '' && (
            <StyledDialog open={true}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">{TranslateText('Inspection report')}</Typography>
                    <StyledCloseButton variant="ghost" onClick={() => setInspectionTask(undefined)}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <StyledInspection id="inspectionCanvas" />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                <Typography variant="body_short">{installationName}</Typography>
                            </StyledInfoContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{task.tagId}</Typography>
                            </StyledInfoContent>
                            {task.description && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description' + ':')}</Typography>
                                    <Typography variant="body_short">{task.description}</Typography>
                                </StyledInfoContent>
                            )}
                            {task.endTime && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestanp' + ':')}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(task.endTime, 'dd.MM.yy - HH:mm')}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                        </StyledBottomContent>
                    </div>
                    <InspectionsViewSection tasks={tasks} setInspectionTask={setInspectionTask} widthValue="150px" />
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>

        )}
        </>
    )
}

interface InspectionsViewSectionProps {
    tasks: Task[]
    setInspectionTask: (inspectionTask: Task | undefined) => void
    widthValue?: string | undefined
}

export const InspectionsViewSection = ({ tasks, setInspectionTask, widthValue }: InspectionsViewSectionProps) => {
    const { TranslateText } = useLanguageContext()
    const imageObjectURL = useRef<string>('')

    return (
        <>
        {imageObjectURL.current != '' && (
            
            <StyledSection
            style={{
                width: widthValue ? widthValue : 'auto',
                borderColor: widthValue ? 'white' : 'auto',
                padding: widthValue ? '0px' : 'auto',
            }}
        >
            {!widthValue && <Typography variant="h4">{TranslateText('Last completed inspection')}</Typography>}
            <StyledImagesSection>
                <StyledInspectionCards>
                    {Object.keys(tasks).length > 0 &&
                        tasks.map(
                            (task) =>
                                task.status === TaskStatus.Successful && (
                                    <StyledImageCard key={task.isarTaskId} onClick={() => setInspectionTask(task)}>
                                        <GetInspectionImage task={task} imageObjectURL={imageObjectURL} />
                                        <StyledInspectionData>
                                            {task.tagId && (
                                                <StyledInspectionContent>
                                                    <Typography variant="caption">
                                                        {TranslateText('Tag') + ':'}
                                                    </Typography>
                                                    <Typography variant="body_short">{task.tagId}</Typography>
                                                </StyledInspectionContent>
                                            )}
                                            {task.endTime && (
                                                <StyledInspectionContent>
                                                    <Typography variant="caption">
                                                        {TranslateText('Timestamp') + ':'}
                                                    </Typography>
                                                    <Typography variant="body_short">
                                                        {formatDateTime(task.endTime!, 'dd.MM.yy - HH:mm')}
                                                    </Typography>
                                                </StyledInspectionContent>
                                            )}
                                        </StyledInspectionData>
                                    </StyledImageCard>
                                )
                        )}
                </StyledInspectionCards>
            </StyledImagesSection>
        </StyledSection>

        )}
        </>
        
    )
}

interface IGetInspectionImageProps {
    task: Task
    imageObjectURL: React.MutableRefObject<string>
}

const GetInspectionImage = ({ task, imageObjectURL }: IGetInspectionImageProps) => {
    const { installationCode } = useInstallationContext()
    const [inspectionCanvas, setInspectionCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [inspectionImage, setInspectionImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [inspectionContext, setInspectionContext] = useState<CanvasRenderingContext2D>()

    useEffect(() => {
        BackendAPICaller.getInspection(installationCode, task.isarTaskId!)
            .then((imageBlob) => {
                imageObjectURL.current = URL.createObjectURL(imageBlob)
            })
            .then(() => {
                getMeta(imageObjectURL.current).then((img) => {
                    const inspectionCanvas = document.getElementById(task.isarTaskId!) as HTMLCanvasElement
                    if (inspectionCanvas) {
                        inspectionCanvas.width = img.width
                        inspectionCanvas.height = img.height
                        let context = inspectionCanvas.getContext('2d')
                        if (context) {
                            setInspectionContext(context)
                            context.drawImage(img, 0, 0)
                        }
                        setInspectionCanvas(inspectionCanvas)
                    }
                    setInspectionImage(img)
                })
            })
            .catch((e) => {})
    }, [installationCode, task.isarTaskId, task])

    return <StyledInspectionImage id={task.isarTaskId} />
}
