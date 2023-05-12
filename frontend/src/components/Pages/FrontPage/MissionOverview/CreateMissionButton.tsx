import { Button, Typography, Popover, Icon } from '@equinor/eds-core-react'
import { TranslateText } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { useRef, useState } from 'react'
import { useAssetContext } from 'components/Contexts/AssetContext'

export const CreateMissionButton = (): JSX.Element => {
    const [isPopoverOpen, setIsPopoverOpen] = useState<boolean>(false)
    const { assetCode } = useAssetContext()
    const anchorRef = useRef<HTMLButtonElement>(null)
    const echoURL = 'https://echo.equinor.com/mp?instCode='

    let timer: ReturnType<typeof setTimeout>
    const openPopover = () => {
        if (assetCode === '') setIsPopoverOpen(true)
    }

    const closePopover = () => setIsPopoverOpen(false)

    const handleHover = () => {
        timer = setTimeout(() => {
            openPopover()
        }, 300)
    }

    const handleClose = () => {
        clearTimeout(timer)
        closePopover()
    }

    return (
        <>
            <div onPointerEnter={handleHover} onPointerLeave={handleClose} onFocus={openPopover} onBlur={handleClose}>
                <Button
                    onClick={() => {
                        window.open(echoURL + assetCode)
                    }}
                    disabled={assetCode === ''}
                    ref={anchorRef}
                >
                    <>
                        <Icon name={Icons.ExternalLink} size={16}></Icon>
                        {TranslateText('Create mission')}
                    </>
                </Button>
            </div>

            <Popover
                anchorEl={anchorRef.current}
                onClose={handleClose}
                open={isPopoverOpen && assetCode === ''}
                placement="top"
            >
                <Popover.Content>
                    <Typography variant="body_short">{TranslateText('Please select asset')}</Typography>
                </Popover.Content>
            </Popover>
        </>
    )
}
