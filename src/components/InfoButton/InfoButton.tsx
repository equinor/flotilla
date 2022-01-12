import React from 'react';
import { info_circle } from '@equinor/eds-icons'
import { Button, Icon } from '@equinor/eds-core-react'
import { Popover } from '@equinor/eds-core-react'
import { useRef, useState } from 'react'

import { tokens } from '@equinor/eds-tokens'

Icon.add({ info_circle })

interface InfoButtonProps {
    title: string
    content: JSX.Element
}

const InfoButton = ({ title, content }: InfoButtonProps): JSX.Element => {
    const [isOpen, setIsOpen] = useState<boolean>(false)
    const anchorRef = useRef<HTMLButtonElement>(null)

    const closePopover = () => setIsOpen(false)
    const toggleOpen = () => setIsOpen(!isOpen)
    return (
        <>
            <Button ref={anchorRef} variant="ghost_icon" onClick={toggleOpen}>
                <Icon name="info_circle" size={24} color={tokens.colors.interactive.primary__resting.hex} />
            </Button>
            <Popover
                id="click-popover"
                aria-expanded={isOpen}
                anchorEl={anchorRef.current}
                onClose={closePopover}
                open={isOpen}
            >
                <Popover.Title>{title}</Popover.Title>
                <Popover.Content>{content}</Popover.Content>
            </Popover>
        </>
    )
}

export default InfoButton
